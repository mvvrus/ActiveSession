﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;


namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionStore : IActiveSessionStore, IDisposable
    {
        const string TYPE_KEY_PART = "_Type";
        const Int32 RUNNERS_TIMEOUT_MSEC = 10000;

        #region InstannceFields
        readonly IMemoryCache _memoryCache;
        readonly IServiceProvider _rootServiceProvider;
        readonly IRunnerManagerFactory _runnerManagerFactory;
        readonly string _prefix;
        readonly string _hostId;
        readonly bool _useOwnCache;
        readonly TimeSpan _idleTimeout;
        readonly TimeSpan _maxLifetime;
        readonly Boolean _throwOnRemoteRunner;
        readonly Boolean _cacheAsTask;
        bool _disposed = false;
        ILogger? _logger;
        readonly Dictionary<FactoryKey, object> _factoryCache = new Dictionary<FactoryKey, object>();
        readonly Object _creation_lock = new Object();
        Int32 _currentSessionCount = 0;
        Int32 _currentRunnerCount = 0;
        Int32 _currentStoreSize = 0;
        Boolean _trackStatistics = false;
        Int32 _activeSessionSize = DEFAULT_ACTIVESESSIONSIZE;
        Int32 _runnerSize = DEFAULT_RUNNERSIZE;
        #endregion

        #region StaticStuff
        static readonly Dictionary<String,Type> s_ResultTypesDictionary = new Dictionary<String,Type>();
        internal static void RegisterTResult(Type TResult)
        {
            s_ResultTypesDictionary.TryAdd(TResult.FullName!, TResult);
        }
        #endregion

        #region PublicAPI
        public ActiveSessionStore(
            IMemoryCache? Cache,
            IServiceProvider RootServiceProvider,
            IRunnerManagerFactory RunnerManagerFactory,
            IOptions<ActiveSessionOptions> Options,
            IOptions<SessionOptions> SessionOptions,
            ILoggerFactory? LoggerFactory = null
        )
        {
            if (Options is null)
                throw new ArgumentNullException(nameof(Options));
            if (SessionOptions is null)
                throw new ArgumentNullException(nameof(SessionOptions));
            _rootServiceProvider= RootServiceProvider??throw new ArgumentNullException(nameof(RootServiceProvider));
            _runnerManagerFactory = RunnerManagerFactory??throw new ArgumentNullException(nameof(RunnerManagerFactory));
            _logger=LoggerFactory?.CreateLogger(LOGGING_CATEGORY_NAME);
            #if TRACE
            _logger?.LogTraceActiveSessionStoreConstructor();
            #endif
            ActiveSessionOptions options = Options.Value;
            _logger?.LogDebugActiveSessionStoreConstructorOptions(options);
            _useOwnCache=options.UseOwnCache;
            if (_useOwnCache)
            {
                #if TRACE
                _logger?.LogTraceLogTraceActiveSessionStoreConstructorCreatingOwnCache();
                #endif
                MemoryCacheOptions own_cache_options = options.OwnCacheOptions??new MemoryCacheOptions();
                _logger?.LogDebugActiveSessionStoreConstructorOwnCacheOptions(new LoggingExtensions.MemoryCacheOptionsForLogging(own_cache_options));

                try {
                    _memoryCache = new MemoryCache(own_cache_options);
                    _logger?.LogDebugActiveSessionStoreConstructorOwnCaheCreated();
                }
                catch(Exception exception)
                {
                    _logger?.LogWarningActiveSessionStoreCannotCreateOwnCache(exception);
                    _useOwnCache=false;
                }
            }
            else
            {
                #if TRACE
                _logger?.LogTraceActiveSessionStoreConstructorUseSharedCache();
                #endif
            }
            _memoryCache ??= Cache!;
            if (_memoryCache == null) {
                _logger?.LogErrorNoSharedCacheException();
                throw new InvalidOperationException("Shared cache must be used but is not available");
            }
            _hostId = options.HostId!;
            _prefix = options.Prefix;
            _idleTimeout = SessionOptions.Value.IdleTimeout;
            _maxLifetime = options.MaxLifetime;
            _throwOnRemoteRunner=options.ThrowOnRemoteRunner;
            _cacheAsTask=options.CacheRunnerAsTask;
            _trackStatistics=options.TrackStatistics;
            _activeSessionSize=options.ActiveSessionSize;
            _runnerSize=options.DefaultRunnerSize;
            #if TRACE
            _logger?.LogTraceActiveSessionStoreConstructorExit();
            #endif
        }

        public void Dispose()
        {
            if (_disposed) return;
            #if TRACE
            _logger?.LogTraceActiveSessionStoreDisposing();
            #endif
            _logger=null;
            _disposed= true;
            if (_useOwnCache) _memoryCache.Dispose();
            GC.SuppressFinalize(this);
        }

        public IActiveSession FetchOrCreateSession(ISession Session, String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceFetchOrCreate(trace_identifier);
            #endif

            ActiveSession result;
            String session_id = Session.Id;
            String key = SessionKey(session_id);
            _logger?.LogDebugActiveSessionKeyToUse(key, trace_identifier);
            if(!Session.Keys.Contains(key)) Session.SetString(key, session_id);
            if (_memoryCache.TryGetValue(key, out result))
                _logger?.LogDebugFoundExistingActiveSession(key, trace_identifier);
            else {
                #if TRACE
                _logger?.LogTraceAcquiringSessionCreationLock(trace_identifier);
                #endif
                Monitor.Enter(_creation_lock);
                try {
                    #if TRACE
                    _logger?.LogTraceAcquiredSessionCreationLock(trace_identifier);
                    #endif
                    if (_memoryCache.TryGetValue(key, out result))
                        _logger?.LogDebugFoundExistingActiveSession(key, trace_identifier);
                    else {
                        _logger?.LogDebugCreateNewActiveSession(key, trace_identifier);
                        try {
                            using (ICacheEntry new_entry = _memoryCache.CreateEntry(key)) { 
                                new_entry.SlidingExpiration=_idleTimeout;
                                new_entry.AbsoluteExpirationRelativeToNow=_maxLifetime;
                                Int32 size = GetSessionSize();
                                new_entry.Size=size;
                                IServiceScope session_scope = _rootServiceProvider.CreateScope();
                                IRunnerManager runner_manager = _runnerManagerFactory.GetRunnerManager(
                                    _logger,
                                    session_scope.ServiceProvider
                                    ); //TODO Set MinRunnerNumber & MaxRunnerNumber
                                RunnerManagerInfo info = new RunnerManagerInfo();
                                PostEvictionCallbackRegistration end_activesession = new PostEvictionCallbackRegistration();
                                end_activesession.EvictionCallback=ActiveSessionEvictionCallback;
                                end_activesession.State=new SessionPostEvictionInfo(session_id, session_scope, runner_manager,info);
                                new_entry.PostEvictionCallbacks.Add(end_activesession);
                                Task runner_completion_task = new Task(RunnerManagerCleanupWait, info);
                                result=new ActiveSession(runner_manager, session_scope, this, Session, _logger, runner_completion_task, trace_identifier);
                                try {
                                    runner_manager.RegisterSession(result);
                                    new_entry.ExpirationTokens.Add(new CancellationChangeToken(result.CompletionToken));
                                    //An assignment to Value property should be the last operation before new_entry.Dispose()
                                    //to avoid adding bad entry to the cache by Dispose() 
                                    new_entry.Value=result;
                                    if(_trackStatistics) {
                                        Interlocked.Increment(ref _currentSessionCount);
                                        Interlocked.Add(ref _currentStoreSize, size);
                                    }
                                }
                                catch {
                                    result.Dispose();
                                    runner_manager.PerformRunnersCleanupAsync(result);
                                    throw;
                                }
                            } //Commit the entry to the cache via implicit Dispose call 
                        }
                        catch (Exception exception) {
                            _logger?.LogDebugFetchOrCreateExceptionalExit(exception, trace_identifier);
                            throw;
                        }
                    }
                }
                finally {
                    #if TRACE
                    _logger?.LogTraceReleasedSessionCreationLock(trace_identifier);
                    #endif
                    Monitor.Exit(_creation_lock);
                }
            }
            #if TRACE
            _logger?.LogTraceFetchOrCreateExit(trace_identifier);
            #endif
            return result;
        }

        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            TRequest Request, 
            String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            String session_id = ActiveSession.Id;
            IActiveSessionRunner<TResult>? runner;
            #if TRACE
            _logger?.LogTraceCreateRunner(trace_identifier);
            #endif
            //TODO LogTrace getting session lock
            Int32 runner_number = -1;
            Boolean use_session_lock = true;
            Object? runner_lock= RunnerManager.RunnerCreationLock; 
            if (runner_lock==null) {
                _logger?.LogTraceFallbackToStoreGlobalLock(session_id, trace_identifier);
                runner_lock=_creation_lock;
                use_session_lock=false;
            }
            #if TRACE
            _logger?.LogTraceAcquiringRunnerCreationLock(use_session_lock ? session_id : "<global>", trace_identifier);
            #endif
            Monitor.Enter(runner_lock);
            try {
                #if TRACE
                _logger?.LogTraceAcquiredRunnerCreationLock(use_session_lock ? session_id : "<global>", trace_identifier);
                #endif
                runner_number= RunnerManager.GetNewRunnerNumber(ActiveSession, trace_identifier);
                String runner_session_key = SessionKey(session_id);
                String runner_key = RunnerKey(runner_session_key, runner_number);
                #if TRACE
                _logger?.LogTraceNewRunnerInfoRunner(runner_session_key, runner_number, trace_identifier);
                #endif
                try {
                    using (ICacheEntry new_entry = _memoryCache.CreateEntry(runner_key)) {
                        new_entry.SlidingExpiration=_idleTimeout;
                        new_entry.AbsoluteExpirationRelativeToNow=_maxLifetime;
                        IActiveSessionRunnerFactory<TRequest, TResult> factory = GetRunnerFactory<TRequest, TResult>(trace_identifier);
                        runner=factory.Create(Request, ActiveSession.SessionServices);
                        if (runner==null) {
                            _logger?.LogErrorCreateRunnerFailure(trace_identifier);
                            throw new InvalidOperationException("The factory failed to create a runner and returned null");
                        }
                        try {
                            _logger?.LogDebugCreateNewRunner(runner_number, trace_identifier);
                            Int32 size = GetRunnerSize(runner.GetType());
                            new_entry.Size=size;
                            PostEvictionCallbackRegistration end_runner = new PostEvictionCallbackRegistration();
                            end_runner.EvictionCallback=RunnerEvictionCallback;
                            end_runner.State=
                                new RunnerPostEvictionInfo(
                                    RunnerManager,
                                    runner_number,
                                    true,
                                    ActiveSession,
                                    runner
                                );
                            new_entry.PostEvictionCallbacks.Add(end_runner);
                            RegisterRunnerInSession(Session, runner_session_key, runner_number, typeof(TResult), trace_identifier);
                            try {

                                IChangeToken expiration_token = new CancellationChangeToken(ActiveSession.CompletionToken);
                                if (runner.GetCompletionToken().CanBeCanceled)
                                    expiration_token=new CompositeChangeToken(new IChangeToken[] { 
                                        expiration_token, 
                                        new CancellationChangeToken(runner.GetCompletionToken()) 
                                    });
                                new_entry.ExpirationTokens.Add(expiration_token);
                                //An assignment to Value property should be the last one before new_entry.Dispose()
                                //to avoid adding bad entry to the cache by Dispose() 
                                new_entry.Value=_cacheAsTask ? Task.FromResult(runner) : runner;
                                try {
                                    RunnerManager.RegisterRunner(ActiveSession, runner_number, runner, typeof(TResult));
                                }
                                catch {
                                    new_entry.Value=null;
                                    throw;
                                }
                                if (_trackStatistics) {
                                    Interlocked.Increment(ref _currentRunnerCount);
                                    Interlocked.Add(ref _currentStoreSize, size);
                                }
                            }
                            catch {
                                UnregisterRunnerInSession(Session, runner_session_key, runner_number);
                                throw;
                            }
                        }
                        catch  {
                            (runner as IDisposable)?.Dispose();
                            throw;
                        }
                    } //Commit the entry to the cache via implicit Dispose call 
                }
                catch (Exception exception) {
                    _logger?.LogDebugCreateRunnerFailure(exception, trace_identifier);
                    throw;
                }

            }
            catch (Exception) {
                if(runner_number>=0) { 
                    #if TRACE
                    _logger?.LogTraceWaiveRunnerNumber(trace_identifier);
                    #endif
                    RunnerManager.ReturnRunnerNumber(ActiveSession, runner_number);
                }
                throw;
            }
            finally {
                _logger?.LogTraceReleasedRunnerCreationLock(use_session_lock ? session_id : "<global>", trace_identifier);
                Monitor.Exit(runner_lock);
            }
            #if TRACE
            _logger?.LogTraceCreateRunnerExit(trace_identifier);
            #endif
            return new KeyedActiveSessionRunner<TResult>() { Runner=runner, RunnerNumber=runner_number };
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            Int32 RunnerNumber, 
            String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            String session_id = ActiveSession.Id;
            String runner_session_key = SessionKey(session_id);
            IActiveSessionRunner<TResult>? result = null;
            #if TRACE
            //Log entrance
            _logger?.LogTraceGetRunner(session_id, RunnerNumber, trace_identifier);
            #endif
            String? host_id;
            String runner_key = RunnerKey(runner_session_key, RunnerNumber);
            host_id=Session.GetString(runner_key);
            if (host_id==null) {
                #if TRACE
                _logger?.LogTraceNoRunnerInSession(trace_identifier);
                #endif
            }
            else if (host_id==_hostId) {
                _logger?.LogDebugGetLocalRunnerFromCache(RunnerNumber, trace_identifier);
                result=ExtractRunnerFromCache<TResult>(Session, RunnerManager, runner_session_key, RunnerNumber, trace_identifier);
            }
            else {
                _logger?.LogDebugProcessRemoteRunner(RunnerNumber, host_id, trace_identifier);
                result=MakeRemoteRunnerAsync<TResult>(RunnerManager, host_id, runner_key, trace_identifier).GetAwaiter().GetResult();
            }
            #if TRACE
            _logger?.LogTraceGetRunnerExit(result!=null,trace_identifier);
            #endif
            return result;
        }

        public async ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        )
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            String session_id = ActiveSession.Id;
            IActiveSessionRunner<TResult>? result = null;
            #if TRACE
            _logger?.LogTraceGetRunnerAsync(trace_identifier);
            #endif
            await Session.LoadAsync(Token);
            String runner_session_key = SessionKey(session_id);
            #if TRACE
            _logger?.LogTraceGetRunnerAsyncInfoRunner(runner_session_key, RunnerNumber, trace_identifier);
            #endif
            String? host_id;
            String runner_key = RunnerKey(runner_session_key, RunnerNumber);
            host_id=Session.GetString(runner_key);
            if (host_id==null) {
                #if TRACE
                _logger?.LogTraceNoRunnerInSession(trace_identifier);
                #endif
                result= null;
            }
            else if (host_id==_hostId) {
                _logger?.LogDebugGetLocalRunnerFromCache(RunnerNumber, trace_identifier);
                result= ExtractRunnerFromCache<TResult>(Session, RunnerManager, runner_session_key, RunnerNumber, trace_identifier);
            }
            else {
                _logger?.LogDebugProcessRemoteRunner(RunnerNumber, host_id, trace_identifier);
                #if TRACE
                _logger?.LogTraceAwaitForProxyCreation(trace_identifier);
                #endif
                result= await MakeRemoteRunnerAsync<TResult>(RunnerManager, runner_key, host_id, trace_identifier, Token);
            }
            #if TRACE
            _logger?.LogTraceGetRunnerAsyncExit(result!=null, trace_identifier);
            #endif
            return result;
        }

        public Task TerminateSession(IActiveSession Session, IRunnerManager RunnerManager, Boolean Global)
        {
            //TODO-Future Implement Global parameter processing
            //TODO LogTrace AbortAll
            RunnerManager.AbortAll(Session);
            (Session as IDisposable)?.Dispose();
            return Session.CleanupCompletionTask;
        }

        public ActiveSessionStoreStats? GetCurrentStatistics()
        {
            return _trackStatistics?new ActiveSessionStoreStats() { 
                SessionCount=_currentSessionCount,
                RunnerCount=_currentRunnerCount,
                StoreSize=_currentStoreSize,
            } :null;
        }

        public IActiveSessionFeature CreateFeatureObject(ISession? Session, String? TraceIdentier)
        {
            return new ActiveSessionFeature(this, Session, _logger, TraceIdentier);
        }
        #endregion

        #region PrivateMethods
        class RunnerManagerInfo
        {
            public Task? CleanupTask;
        }

        void RunnerManagerCleanupWait(Object? State)
        {
            RunnerManagerInfo state =State as RunnerManagerInfo??throw new ArgumentNullException(nameof(State));
            Task cleanup_task = state.CleanupTask??throw new NullReferenceException("State.CleanupTask is null.");

            state.CleanupTask!.Wait();
        }

        private void ActiveSessionEvictionCallback(object Key, object Value, EvictionReason Reason, object State)
        {
            SessionPostEvictionInfo session_info = (SessionPostEvictionInfo)State;
            String session_id = session_info.SessionId??UNKNOWN_SESSION_ID;
            #if TRACE
            _logger?.LogTraceSessionEvictionCallback(session_id);
            #endif
            ActiveSession active_session = (ActiveSession)Value;
            if (_trackStatistics) {
                Interlocked.Decrement(ref _currentSessionCount);
                Interlocked.Add(ref _currentStoreSize, -GetSessionSize());
            }
            #if TRACE
            _logger?.LogTraceAbortRunners(session_id);
            #endif
            session_info.RunnerManager.AbortAll(active_session);
            _logger?.LogDebugBeforeSessionDisposing(session_id);
            active_session.Dispose();
            //Start runners cleanup processing and a task in the ActiveSession waiting for its completion
            #if TRACE
            _logger?.LogTraceSessionCleanupRunnersInitiated(session_id);
            #endif
            Task runners_cleanup_task = session_info.RunnerManager.PerformRunnersCleanupAsync(active_session);
            session_info.RunnerMangerInfo.CleanupTask=runners_cleanup_task;
            active_session.CleanupCompletionTask.Start();
            //Start logger task waiting for a specified time while the runners complete
            TimeoutLoggerInfo tl_info = new TimeoutLoggerInfo(active_session.Id, runners_cleanup_task);
            Task.WhenAny(active_session.CleanupCompletionTask, Task.Delay(RUNNERS_TIMEOUT_MSEC))
                .ContinueWith(TimeoutLoggerBody, tl_info);
            #if TRACE
            _logger?.LogTraceSessionScopeToBeDisposed(session_id);
            #endif
            session_info?.SessionScope.Dispose();
            Object dummy;
            _memoryCache.TryGetValue(Key, out dummy); //To start cache entry expiration checks
            #if TRACE
            _logger?.LogTraceSessionEvictionCallbackExit(session_id);
            #endif
        }

        record TimeoutLoggerInfo(String ActiveSessionId, Task CleanupTask);

        void TimeoutLoggerBody(Task<Task> FirstCompletedInfo, Object ?State)
        {
            TimeoutLoggerInfo state = (TimeoutLoggerInfo)State!;
            #if TRACE
            _logger?.LogTraceActiveSessionCompleteDispose(state.ActiveSessionId); 
            #endif
            Boolean wait_succeded = FirstCompletedInfo.Result==state.CleanupTask;
            #if TRACE
            _logger?.LogTraceActiveSessionEndWaitingForRunnersCompletion(state.ActiveSessionId, wait_succeded);
            #endif
        }

        private IActiveSessionRunnerFactory<TRequest, TResult> GetRunnerFactory<TRequest, TResult>(String TraceIdentifier)
        {
            #if TRACE
            _logger?.LogTraceGetRunnerFactory(TraceIdentifier);
            #endif
            FactoryKey key = new FactoryKey(typeof(TRequest), typeof(TResult));
            String request_type_name = "<unknown>";
            String result_type_name = "<unknown>";
            if (_logger!=null&&_logger!.IsEnabled(LogLevel.Debug)) {
                request_type_name=key.TRequest.FullName!;
                result_type_name=key.TResult.FullName!;
            }
            IActiveSessionRunnerFactory<TRequest, TResult>? factory = null;
            if (_factoryCache!=null) {
                if (_factoryCache.ContainsKey(key)) {
                    factory=(IActiveSessionRunnerFactory<TRequest, TResult>)_factoryCache[key];
                    _logger?.LogDebugGetRunnerFactoryFromCache(request_type_name, result_type_name, TraceIdentifier);
                }
            }
            if (factory==null) {
                factory=_rootServiceProvider.GetRequiredService<IActiveSessionRunnerFactory<TRequest, TResult>>();
                _logger?.LogDebugInstatiateNewRunnerFactory(request_type_name, result_type_name, TraceIdentifier);
                if (_factoryCache!=null) {
                    #if TRACE
                    _logger?.LogTraceStoreNewRunnerFactoryInCache(TraceIdentifier);
                    #endif
                    _factoryCache.TryAdd(key, factory);
                }
            }
            #if TRACE
            _logger?.LogTraceGetRunnerFactoryExit(TraceIdentifier);
            #endif
            return factory;
        }

        private void RunnerEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            RunnerPostEvictionInfo runner_info = (RunnerPostEvictionInfo)state;
            #if TRACE
            _logger?.LogTraceRunnerEvictionCallback(runner_info.ActiveSession.Id, runner_info.Number);
            #endif
            //Do not call UnregisterRunnerInSession here because the session is inaccessible here and may even be destroyed
            //One remove session variables of an unexisting runner while searching for it and cannot find it in the cache
            IActiveSessionRunner runner = runner_info.Runner;
            runner_info.RunnerManager.UnregisterRunner(runner_info.ActiveSession, runner_info.Number);
            runner_info.RunnerManager.ReturnRunnerNumber(runner_info.ActiveSession, runner_info.Number);
            runner.Abort();
            if (_trackStatistics) {
                Interlocked.Decrement(ref _currentRunnerCount);
                if(runner!=null) Interlocked.Add(ref _currentStoreSize, -GetRunnerSize(runner.GetType()));
            }
            #if TRACE
            _logger?.LogTraceRunnerEvictionCallbackExit(runner_info.ActiveSession.Id, runner_info.Number);
            #endif
        }

        //Methods to manage runner information in the session (ISession implementation)
        //
        //Key-value pairs to register/unregister
        // ( {RuunerKey}=_prefix+"_"+session_id+"_"+RunnerNumber.ToString() )
        // {RunnerKey} = _hostId
        // {RunnerKey}+"_Type" = ResultType
        private void RegisterRunnerInSession(ISession Session, String RunnerSessionKey, Int32 RunnerNumber, Type ResultType, String TraceIdentifier)
        {
            #if TRACE
            _logger?.LogTraceRegisterRunnerInSession(RunnerSessionKey, RunnerNumber, TraceIdentifier);
            #endif
            String runner_key = RunnerKey(RunnerSessionKey, RunnerNumber);
            Session.SetString(runner_key, _hostId); 
            Session.SetString(runner_key+TYPE_KEY_PART, ResultType.FullName!);
            #if TRACE
            _logger?.LogTraceRegisterRunnerInSessionExit(TraceIdentifier);
            #endif
        }

        private void UnregisterRunnerInSession(ISession Session, String RunnerSessionKey, Int32 RunnerNumber)
        {
            #if TRACE
            _logger?.LogTraceUnregisterRunnerInSession(Session?.Id??UNKNOWN_SESSION_ID, RunnerNumber);
            #endif
            if (Session?.IsAvailable??false) {
                #if TRACE
                _logger?.LogTracePerformUnregisteration(Session?.Id??UNKNOWN_SESSION_ID, RunnerNumber);
                #endif
                String runner_key = RunnerKey(RunnerSessionKey, RunnerNumber);
                Session!.Remove(runner_key);
                Session!.Remove(runner_key+TYPE_KEY_PART);
            }
            #if TRACE
            _logger?.LogTraceUnregisterRunnerInSessionExit(Session?.Id??UNKNOWN_SESSION_ID, RunnerNumber);
            #endif
        }

        private IActiveSessionRunner<TResult>? ExtractRunnerFromCache<TResult>(ISession Session, 
            IRunnerManager RunnerManager, String RunnerSessionKey, Int32 RunnerNumber, String TraceIdentifier)
        {
            Object? value_from_cache;
            IActiveSessionRunner<TResult>? result;
            #if TRACE
            _logger?.LogTraceExtractRunnerFromCache(Session.Id, RunnerNumber, TraceIdentifier);
            #endif
            String runner_key = RunnerKey(RunnerSessionKey, RunnerNumber);
            if (_memoryCache.TryGetValue(runner_key, out value_from_cache)) {
                #if TRACE
                _logger?.LogTraceReturnRunnerFromCache(TraceIdentifier);
                #endif
                result= _cacheAsTask ?
                    //Note: the cache alwais contains a completed task so the next line always executes synchronously
                    (value_from_cache as Task<IActiveSessionRunner<TResult>?>)?.Result :
                    value_from_cache as IActiveSessionRunner<TResult>;
                if (result==null)  _logger?.LogWarningNoExpectedRunnerInCache(TraceIdentifier);
            }
            else {
                #if TRACE
                _logger?.LogTraceNoExpectedRunnerInCache(TraceIdentifier);
                #endif
                //Remove values connected with previosly evicted runner.
                //One could not do it from runner eviction callback because the session was not available there
                UnregisterRunnerInSession(Session, RunnerSessionKey, RunnerNumber);
                result = null;
            }
            #if TRACE
            _logger?.LogTraceExtractRunnerFromCacheExit(TraceIdentifier);
            #endif
            return result;
        }


        private Task<IActiveSessionRunner<TResult>?> MakeRemoteRunnerAsync<TResult>(
#pragma warning disable IDE0060 // Remove unused parameter
            IRunnerManager RunnerManager,
            String HostId,
            String RunnerKey,
            String TraceIdentifier,
            CancellationToken Token=default
#pragma warning restore IDE0060 // Remove unused parameter
        )
        {
            _logger?.LogWarningRemoteRunnerUnavailable(TraceIdentifier);
            if (_throwOnRemoteRunner) {
                _logger?.LogErrorRemoteRunnerUnavailable(TraceIdentifier);
                throw new InvalidOperationException("Using remote runners is not allowed  configuration setting ThrowOnRemoteRunner");
            }
            return Task.FromResult<IActiveSessionRunner<TResult>?>(null); //Just now I do not want to implement remote runner

            //Possible future implementation draft
            //String? runner_type_name = RunnerManager.Session.GetString(RunnerKey+TYPE_KEY_PART);
            //if (runner_type_name==null) {
            //    //TODO? Log that runner has unknown type
            //    return null;
            //}
            //if (!s_ResultTypesDictionary.TryGetValue(runner_type_name, out runner_type)) {
            //    //TODO? Log that type is unregistered
            //    throw new InvalidOperationException(); //TODO Add message
            //}
            //if (runner_type.IsAssignableTo(typeof(TResult))) { } //TODO
            //                                                     //TODO
            //throw new NotImplementedException();
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(this.GetType().FullName);
        }

        String SessionKey(String SessionId)
        {
            return $"{_prefix}_{SessionId}";
        }

        String RunnerKey(String SessionKey, Int32 RunnerNumber)
        {
            return $"{SessionKey}_{RunnerNumber}";
        }

        Int32 GetSessionSize()
        {
            return _activeSessionSize; 
        }

        Int32 GetRunnerSize(Type RunnerType)
        {
            return _runnerSize; //TODO: it's a stub
        }

        #endregion

        #region AuxilaryTypes
        readonly record struct FactoryKey(Type TRequest, Type TResult);

        internal class RunnerPostEvictionInfo
        {
            public IRunnerManager RunnerManager;
            public IActiveSession ActiveSession;
            public Int32 Number;
            public Boolean UnregisterNumber;
            public IActiveSessionRunner Runner;

            public RunnerPostEvictionInfo(
                IRunnerManager RunnerManager,
                Int32 Number,
                Boolean UnregisterNumber,
                IActiveSession ActiveSession,
                IActiveSessionRunner Runner
            )
            {
                this.RunnerManager=RunnerManager;
                this.Number=Number;
                this.UnregisterNumber=UnregisterNumber;
                this.ActiveSession= ActiveSession;
                this.Runner=Runner;
            }
        }

        record SessionPostEvictionInfo(String SessionId, IServiceScope SessionScope, IRunnerManager RunnerManager, RunnerManagerInfo RunnerMangerInfo);
        #endregion
    }
}
