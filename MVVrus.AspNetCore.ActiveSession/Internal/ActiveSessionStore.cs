using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;


namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionStore : IActiveSessionStore, IDisposable
    {
        const string TYPE_KEY_PART = "_Type";

        #region InstannceFields
        readonly IMemoryCache _memoryCache;
        readonly IServiceProvider _rootServiceProvider;
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
            IMemoryCache Cache,
            IServiceProvider RootServiceProvider,
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
            _memoryCache=_memoryCache ?? Cache;
            if (_memoryCache == null) {
                _logger?.LogErrorNoSharedCacheException();
                throw new InvalidOperationException("Shared cache must be used but is not available");
            }
            _hostId= options.HostId!;
            _prefix = options.Prefix;
            _idleTimeout = SessionOptions.Value.IdleTimeout;
            _maxLifetime = options.MaxLifetime;
            _throwOnRemoteRunner=options.ThrowOnRemoteRunner;
            _cacheAsTask=options.CacheRunnerAsTask;
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

        public ActiveSession FetchOrCreateSession(ISession Session, String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceFetchOrCreate(trace_identifier);
            #endif

            ActiveSession result;
            String key = SessionKey(Session.Id);
            _logger?.LogDebugActiveSessionKeyToUse(key, trace_identifier);
            if(!Session.Keys.Contains(key)) Session.SetString(key, Session.Id);
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
                        ICacheEntry new_entry = _memoryCache.CreateEntry(key);
                        try {
                            new_entry.SlidingExpiration=_idleTimeout;
                            new_entry.AbsoluteExpirationRelativeToNow=_maxLifetime;
                            new_entry.Size=1; //TODO Size from config?
                            result=new ActiveSession(_rootServiceProvider.CreateScope(), this, Session, _logger, trace_identifier);
                            try {
                                new_entry.SetValue(result);
                                PostEvictionCallbackRegistration end_activesession = new PostEvictionCallbackRegistration();
                                end_activesession.EvictionCallback=EndActiveSessionCallback;
                                end_activesession.State=Session.Id;
                                new_entry.PostEvictionCallbacks.Add(end_activesession);
                                new_entry.Dispose(); //Commit entry to the cache
                            }
                            catch {
                                result.Dispose();
                                throw;
                            }

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
            ActiveSession RunnerSession,
            TRequest Request, 
            String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            IActiveSessionRunner<TResult>? runner;
            #if TRACE
            _logger?.LogTraceCreateRunner(trace_identifier);
            #endif
            Int32 runner_number = -1;
            Boolean use_session_lock = true;
            Object? runner_lock= (Session as ActiveSession)?.RunnerCreationLock; 
            if (runner_lock==null) {
                _logger?.LogTraceFallbackToStoreGlobalLock(Session.Id, trace_identifier);
                runner_lock=_creation_lock;
                use_session_lock=false;
            }
            #if TRACE
            _logger?.LogTraceAcquiringRunnerCreationLock(use_session_lock ? Session.Id : "<global>", trace_identifier);
            #endif
            Monitor.Enter(runner_lock);
            try {
                #if TRACE
                _logger?.LogTraceAcquiredRunnerCreationLock(use_session_lock ? Session.Id : "<global>", trace_identifier);
                #endif
                runner_number= RunnerSession.GetNewRunnerNumber(trace_identifier);
                String runner_key = RunnerKey(Session, runner_number);
                String runner_session_key = SessionKey(Session.Id);
                #if TRACE
                _logger?.LogTraceNewRunnerInfoRunner(runner_session_key, runner_number, trace_identifier);
                #endif
                ICacheEntry new_entry = _memoryCache.CreateEntry(runner_key);
                try {
                    new_entry.SlidingExpiration=_idleTimeout;
                    new_entry.AbsoluteExpirationRelativeToNow=_maxLifetime;
                    new_entry.Size=1; //TODO Size from config?
                    IActiveSessionRunnerFactory<TRequest, TResult> factory = GetRunnerFactory<TRequest, TResult>(trace_identifier);
                    runner=factory.Create(Request, RunnerSession.Services);
                    if (runner==null) {
                        _logger?.LogErrorCreateRunnerFailure(trace_identifier);
                        throw new InvalidOperationException("The factory failed to create a runner and returned null");
                    }
                    try {
                        _logger?.LogDebugCreateNewRunner(runner_number, trace_identifier);
                        new_entry.Value=_cacheAsTask ? Task.FromResult(runner) : runner;
                        IChangeToken runner_completion_token = runner.GetCompletionToken();
                        if (runner_completion_token.ActiveChangeCallbacks) {
                            #if TRACE
                            _logger?.LogTraceSettingRunnerCompletionCallback(trace_identifier);
                            #endif
                            runner_completion_token.RegisterChangeCallback(
                                RunnerCompletionCallback,
                                new RunnerPostCompletionInfo(runner, runner_session_key, runner_number)
                            );
                        }
                        PostEvictionCallbackRegistration end_runner = new PostEvictionCallbackRegistration();
                        end_runner.EvictionCallback=RunnerEvictionCallback;
                        end_runner.State=
                            new RunnerPostEvictionInfo(
                                RunnerSession,
                                runner as IDisposable,
                                runner_number,
                                true,
                                runner_session_key
                            );
                        new_entry.PostEvictionCallbacks.Add(end_runner);
                        RunnerSession.RegisterRunner(runner_number);
                        RegisterRunnerInSession(Session, runner_session_key, runner_number, typeof(TResult), trace_identifier);
                        new_entry.Dispose(); //Commit entry to the cache

                    }
                    catch  {
                        (runner as IDisposable)?.Dispose();
                        throw;
                    }                }
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
                    RunnerSession.ReturnRunnerNumber(runner_number);
                }
                throw;
            }
            finally {
                _logger?.LogTraceReleasedRunnerCreationLock(use_session_lock ? Session.Id : "<global>", trace_identifier);
                Monitor.Exit(runner_lock);
            }
            #if TRACE
            _logger?.LogTraceCreateRunnerExit(trace_identifier);
            #endif
            return new KeyedActiveSessionRunner<TResult>() { Runner=runner, RunnerNumber=runner_number };
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(ISession Session, 
            ActiveSession RunnerSession,
            Int32 RunnerNumber, 
            String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            String runner_session_key = SessionKey(Session.Id);
            IActiveSessionRunner<TResult>? result = null;
            #if TRACE
            //Log entrance
            _logger?.LogTraceGetRunner(runner_session_key, RunnerNumber, trace_identifier);
            #endif
            String? host_id;
            String runner_key = RunnerKey(Session, RunnerNumber);
            host_id=Session.GetString(runner_key);
            if (host_id==null) {
                #if TRACE
                _logger?.LogTraceNoRunnerInSession(trace_identifier);
                #endif
            }
            else if (host_id==_hostId) {
                _logger?.LogDebugGetLocalRunnerFromCache(RunnerNumber, trace_identifier);
                result=ExtractRunnerFromCache<TResult>(Session, RunnerSession, runner_session_key, RunnerNumber, trace_identifier);
            }
            else {
                _logger?.LogDebugProcessRemoteRunner(RunnerNumber, host_id, trace_identifier);
                result=MakeRemoteRunnerAsync<TResult>(RunnerSession, host_id, runner_key, trace_identifier).GetAwaiter().GetResult();
            }
            #if TRACE
            _logger?.LogTraceGetRunnerExit(result!=null,trace_identifier);
            #endif
            return result;
        }

        public async ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            ISession Session, ActiveSession RunnerSession,
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        )
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            IActiveSessionRunner<TResult>? result = null;
            #if TRACE
            _logger?.LogTraceGetRunnerAsync(trace_identifier);
            #endif
            await Session.LoadAsync(Token);
            String runner_session_key = SessionKey(Session.Id);
            #if TRACE
            _logger?.LogTraceGetRunnerAsyncInfoRunner(runner_session_key, RunnerNumber, trace_identifier);
            #endif
            String? host_id;
            String runner_key = RunnerKey(Session, RunnerNumber);
            host_id=Session.GetString(runner_key);
            if (host_id==null) {
                #if TRACE
                _logger?.LogTraceNoRunnerInSession(trace_identifier);
                #endif
                result= null;
            }
            else if (host_id==_hostId) {
                _logger?.LogDebugGetLocalRunnerFromCache(RunnerNumber, trace_identifier);
                result= ExtractRunnerFromCache<TResult>(Session, RunnerSession, runner_session_key, RunnerNumber, trace_identifier);
            }
            else {
                _logger?.LogDebugProcessRemoteRunner(RunnerNumber, host_id, trace_identifier);
                #if TRACE
                _logger?.LogTraceAwaitForProxyCreation(trace_identifier);
                #endif
                result= await MakeRemoteRunnerAsync<TResult>(RunnerSession, runner_key, host_id, trace_identifier, Token);
            }
            #if TRACE
            _logger?.LogTraceGetRunnerAsyncExit(result!=null, trace_identifier);
            #endif
            return result;
        }
        #endregion

        #region PrivateMethods
        private void EndActiveSessionCallback(object key, object value, EvictionReason reason, object state)
        {
            ActiveSession? active_session = value as ActiveSession;
            String session_key = state as String??UNKNOWN_SESSION_KEY;
            #if TRACE
            _logger?.LogTraceSessionEvictionCallback(session_key);
            #endif
            if (active_session!=null) {
                #if TRACE
                _logger?.LogTraceEvictRunners(session_key);
                #endif
                active_session.SignalCompletion(); //To evict all runners of the session
                _logger?.LogDebugBeforeSessionDisposing(session_key);
                active_session.Dispose();
            }
            #if TRACE
            _logger?.LogTraceSessionEvictionCallbackExit(session_key);
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

        private void RunnerCompletionCallback(Object obj)
        {
            RunnerPostCompletionInfo runner_info = (RunnerPostCompletionInfo)obj;
            #if TRACE
            _logger?.LogTraceRunnerCompletionCallback(runner_info.RunnerSessionKey, runner_info.RunnerNumber);
            #endif
            String runner_key = RunnerKey(runner_info.RunnerSessionKey, runner_info.RunnerNumber);
            ActiveSessionRunnerState state = runner_info.Runner.State;
            switch (state) {
                case ActiveSessionRunnerState.Aborted:
                    #if TRACE
                    _logger?.LogTraceRunnerCompletionRemoveAborted(runner_info.RunnerSessionKey, runner_info.RunnerNumber);
                    #endif
                    _memoryCache.Remove(runner_key);
                    break;
                default:
                    break;
            }
            #if TRACE
            _logger?.LogTraceRunnerCompletionCallbackExit(runner_info.RunnerSessionKey, runner_info.RunnerNumber);
            #endif
        }

        private void RunnerEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            RunnerPostEvictionInfo runner_info = (RunnerPostEvictionInfo)state;
            #if TRACE
            _logger?.LogTraceRunnerEvictionCallback(runner_info.RunnerSessionKey, runner_info.Number);
            #endif
            if (runner_info.Disposable!=null) {
                #if TRACE
                _logger?.LogTraceDisposeRunner(runner_info.RunnerSessionKey, runner_info.Number);
                #endif
                runner_info.Disposable!.Dispose();
            }
            runner_info.RunnerSession.UnregisterRunner(runner_info.Number);
            #if TRACE
            _logger?.LogTraceRunnerEvictionCallbackExit(runner_info.RunnerSessionKey, runner_info.Number);
            #endif
        }

        //Methods to manage runner information in the session (ISession implementation)
        //
        //Key-value pairs to register/unregister
        // ( {RuunerKey}=_prefix+"_"+Session.Id+"_"+RunnerNumber.ToString() )
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
            _logger?.LogTraceUnregisterRunnerInSession(RunnerSessionKey, RunnerNumber);
            #endif
            if (Session.IsAvailable) {
                #if TRACE
                _logger?.LogTracePerformUnregisteration(RunnerSessionKey, RunnerNumber);
                #endif
                String runner_key = RunnerKey(RunnerSessionKey, RunnerNumber);
                Session.Remove(runner_key);
                Session.Remove(runner_key+TYPE_KEY_PART);
            }
            #if TRACE
            _logger?.LogTraceUnregisterRunnerInSessionExit(RunnerSessionKey, RunnerNumber);
            #endif
        }

        private IActiveSessionRunner<TResult>? ExtractRunnerFromCache<TResult>(ISession Session, 
            ActiveSession RunnerSession, String RunnerSessionKey, Int32 RunnerNumber, String TraceIdentifier)
        {
            Object? value_from_cache;
            IActiveSessionRunner<TResult>? result;
            #if TRACE
            _logger?.LogTraceExtractRunnerFromCache(RunnerSessionKey, RunnerNumber, TraceIdentifier);
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
            ActiveSession RunnerSession,
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
            //String? runner_type_name = RunnerSession.Session.GetString(RunnerKey+TYPE_KEY_PART);
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

        String RunnerKey(ISession Session, Int32 RunnerNumber)
        {
            return $"{SessionKey(Session.Id)}_{RunnerNumber}";
        }
        #endregion

        #region AuxilaryTypes
        readonly record struct FactoryKey(Type TRequest, Type TResult);

        internal class RunnerPostEvictionInfo
        {
            public ActiveSession RunnerSession;
            public IDisposable? Disposable;
            public String RunnerSessionKey;
            public Int32 Number;
            public Boolean UnregisterNumber;

            public RunnerPostEvictionInfo(
                ActiveSession RunnerSession, 
                IDisposable? Disposable, 
                Int32 Number, 
                Boolean UnregisterNumber, 
                String RunnerSessionKey
            )
            {
                this.RunnerSession=RunnerSession;
                this.Disposable=Disposable;
                this.Number=Number;
                this.UnregisterNumber=UnregisterNumber;
                this.RunnerSessionKey= RunnerSessionKey;
            }
        }

        private class RunnerPostCompletionInfo
        {
            internal String RunnerSessionKey;
            internal IActiveSessionRunner Runner;
            internal Int32 RunnerNumber;

            public RunnerPostCompletionInfo(IActiveSessionRunner Runner, String RunnerSessionKey, Int32 RunnerNumber)
            {
                this.Runner=Runner;
                this.RunnerSessionKey=RunnerSessionKey;
                this.RunnerNumber=RunnerNumber;
            }
        }
        #endregion
    }
}
