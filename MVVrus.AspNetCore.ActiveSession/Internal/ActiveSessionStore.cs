using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
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
        internal const Int32 DISPOSE_TIMEOUT = 10000;

        #region InstannceFields
        readonly IMemoryCache _memoryCache;
        readonly IServiceProvider _rootServiceProvider;
        readonly IRunnerManagerFactory _runnerManagerFactory;
        readonly string _prefix;
        readonly string _hostId;
        readonly bool _useOwnCache;
        readonly TimeSpan _sessionIdleTimeout;
        readonly TimeSpan _runnerIdleTimeout;
        readonly TimeSpan _maxLifetime;
        readonly Boolean _throwOnRemoteRunner;
        bool _disposed = false;
        internal Boolean _disposeNoTimedOut;
        ILogger? _logger;
        ILoggerFactory? _loggerFactory;
        readonly Dictionary<FactoryKey, object> _factoryCache = new Dictionary<FactoryKey, object>();
        readonly Object _creation_lock = new Object();
        Int32 _currentSessionCount = 0;
        Int32 _currentRunnerCount = 0;
        Int32 _currentStoreSize = 0;
        Boolean _trackStatistics = false;
        Int32 _activeSessionSize = DEFAULT_ACTIVESESSIONSIZE;
        Int32 _runnerSize = DEFAULT_RUNNERSIZE;
        readonly int? _cleanupLoggingTimeoutMs;
        private readonly Task _storeTask;
        internal Task? _cleanupLoggingTask;  //For unit tests only, no need to consider any parallelism
        readonly SortedSet<String> _sessionKeys;
        readonly TaskCompletionSource _shutdownTcs;
        CancellationTokenRegistration _shutdownCallback;
        volatile Boolean _draining = false;
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
            IHostApplicationLifetime HostApplicationLifetime,
            ILoggerFactory? LoggerFactory = null
        )
        {
            if (Options is null)
                throw new ArgumentNullException(nameof(Options));
            if (SessionOptions is null)
                throw new ArgumentNullException(nameof(SessionOptions));
            _sessionKeys=new SortedSet<String>();
            _rootServiceProvider= RootServiceProvider??throw new ArgumentNullException(nameof(RootServiceProvider));
            _runnerManagerFactory = RunnerManagerFactory??throw new ArgumentNullException(nameof(RunnerManagerFactory));
            _shutdownTcs=new TaskCompletionSource();
            _shutdownCallback=HostApplicationLifetime.ApplicationStopping.Register(() => _shutdownTcs.TrySetResult());
            _loggerFactory=LoggerFactory;
            _logger =_loggerFactory?.CreateLogger(STORE_CATEGORY_NAME);
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
                    _memoryCache = LoggerFactory!=null? new MemoryCache(own_cache_options,LoggerFactory!) : new MemoryCache(own_cache_options);
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
            _sessionIdleTimeout = SessionOptions.Value.IdleTimeout;
            _runnerIdleTimeout=options.RunnerIdleTimeout;
            _maxLifetime = options.MaxLifetime;
            _throwOnRemoteRunner=options.ThrowOnRemoteRunner;
            _trackStatistics=options.TrackStatistics;
            _activeSessionSize=options.ActiveSessionSize;
            _runnerSize=options.DefaultRunnerSize;
            _cleanupLoggingTimeoutMs=options.CleanupLoggingTimeoutMs;
            _storeTask = MaintainCache();
            #if TRACE
            _logger?.LogTraceActiveSessionStoreConstructorExit();
            #endif
        }

        async Task MaintainCache()
        {
            Task completed;
            do {
                completed = await Task.WhenAny(Task.Delay(_sessionIdleTimeout), _shutdownTcs.Task);
                if(completed!=_shutdownTcs.Task) {
                    InitCacheExpiration();
                }
            }
            while(completed!=_shutdownTcs.Task);
            //Come here if the application is stopping
            List<Task> session_completion_tasks=new List<Task>();
            lock(_creation_lock) {
                _draining=true;
                List<String> session_keys = _sessionKeys.ToList(); //Make a stable copy to iterate through
                foreach(String key in session_keys) {
                    Object? cached = null;
                    if(_memoryCache?.TryGetValue(key,out cached)??false) {
                        ActiveSession? session = cached as ActiveSession;
                        if(session!=null) {
                            session_completion_tasks.Add(session.CleanupCompletionTask);
                            DoTerminateSession(session, UNKNOWN_TRACE_IDENTIFIER);
                        }
                    }
                }
            }
            InitCacheExpiration(); //Perform eviction of all runners left in the cache 
            await Task.WhenAll(session_completion_tasks.ToArray());
        }

        public void Dispose()
        {
            if (_disposed) return;
            #if TRACE
            _logger?.LogTraceActiveSessionStoreDisposing();
            #endif
            _disposed= true;
            _shutdownTcs.TrySetResult();
            DateTime dispose_start_time = DateTime.Now;
            TimeSpan dispose_timeout = TimeSpan.FromMilliseconds(DISPOSE_TIMEOUT);
            _disposeNoTimedOut = _storeTask.Wait(dispose_timeout);
            if(_disposeNoTimedOut) {
                dispose_timeout-=DateTime.Now-dispose_start_time;
                _disposeNoTimedOut = _shutdownCallback.DisposeAsync().AsTask()
                    .Wait(Math.Max((Int32)dispose_timeout.TotalMilliseconds,0));
            }
            _logger=null;
            if (_useOwnCache) _memoryCache.Dispose();
            GC.SuppressFinalize(this);
        }

        public IActiveSession? FetchOrCreateSession(ISession Session, String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            String nogen_session_id = Session.Id;
            String session_id = Session.Id+":?";
            #if TRACE
            _logger?.LogTraceFetchOrCreate(nogen_session_id, trace_identifier);
            #endif

            ActiveSession? result=null;
            String key = SessionKey(nogen_session_id);
            _logger?.LogDebugActiveSessionKeyToUse(nogen_session_id, trace_identifier);
            Int32 insession_generation = Session.GetInt32(key)??0;
            Int32 new_generation= insession_generation<=0?-insession_generation+1:0;

            if (_memoryCache.TryGetValue(key, out result)) FoundInCahe();
            if(result==null) { //Not found or evicted due to bad generation
                #if TRACE
                _logger?.LogTraceAcquiringSessionCreationLock(nogen_session_id, trace_identifier);
                #endif
                Monitor.Enter(_creation_lock);
                try {
                    #if TRACE
                    _logger?.LogTraceAcquiredSessionCreationLock(nogen_session_id, trace_identifier);
                    #endif
                    if(_draining) return null;  //The store is stopping due to the application stopping cannot create more sessions
                    if(_memoryCache.TryGetValue(key, out result)) FoundInCahe(); //Repeat check under the lock
                    if(result==null) {  //The requested active session is not found or has been evicted due to bad Generation 
                        session_id = LoggingExtensions.MakeSessionId(nogen_session_id, new_generation);
                        _logger?.LogDebugCreateNewActiveSession(session_id, trace_identifier);
                        try {
                            _sessionKeys.Add(key);
                            using (ICacheEntry new_entry = _memoryCache.CreateEntry(key)) { 
                                new_entry.SlidingExpiration=_sessionIdleTimeout;
                                new_entry.AbsoluteExpirationRelativeToNow=_maxLifetime;
                                Int32 size = GetSessionSize();
                                new_entry.Size=size;
                                IServiceScope session_scope = _rootServiceProvider.CreateScope();
                                IRunnerManager runner_manager = _runnerManagerFactory.GetRunnerManager(
                                    _loggerFactory?.CreateLogger(RUNNERMANAGER_CATEGORY_NAME),
                                    session_scope.ServiceProvider
                                    ); //TODO(future) Set MinRunnerNumber & MaxRunnerNumber
                                RunnerManagerInfo info = new RunnerManagerInfo();
                                PostEvictionCallbackRegistration end_activesession = new PostEvictionCallbackRegistration();
                                end_activesession.EvictionCallback=ActiveSessionEvictionCallback;
                                end_activesession.State=new SessionPostEvictionInfo(session_id, session_scope, runner_manager,info);
                                new_entry.PostEvictionCallbacks.Add(end_activesession);
                                Task runner_completion_task = new Task(RunnerManagerCleanupWait, info);
                                result=new ActiveSession(runner_manager, session_scope, this, Session.Id, _loggerFactory?.CreateLogger(SESSION_CATEGORY_NAME),
                                    new_generation, runner_completion_task, trace_identifier);
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
                                Session.SetInt32(key, new_generation);
                            } //Commit the entry to the cache via implicit Dispose call 
                        }
                        catch (Exception exception) {
                            _logger?.LogDebugFetchOrCreateExceptionalExit(exception, session_id, trace_identifier);
                            _sessionKeys.Remove(key);
                            throw;
                        }
                        CleanupOutdatedRunnerVars(Session, key, new_generation, nogen_session_id, trace_identifier);
                    }
                    else  session_id = result.MakeSessionId();
                }
                finally {
                    #if TRACE
                    _logger?.LogTraceReleasedSessionCreationLock(session_id, trace_identifier);
                    #endif
                    Monitor.Exit(_creation_lock);
                }
            }

            #if TRACE
            _logger?.LogTraceFetchOrCreateExit(session_id, trace_identifier);
            #endif
            return result;

            void FoundInCahe()
            {
                String session_id=result!.MakeSessionId();
                if (result!.Generation<new_generation) {
                    #if TRACE
                    _logger?.LogTraceFetchOrCreateOutdatedSessionFound(session_id, trace_identifier);
                    #endif
                    DoTerminateSession(result!, trace_identifier);
                    result=null;
                    _logger?.LogDebugFoundTerminatedActiveSession(session_id, trace_identifier);
                }
                else
                    _logger?.LogDebugFoundExistingActiveSession(session_id, trace_identifier);

            }

        }

        public KeyedRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            TRequest Request, 
            String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            String session_id = ActiveSession.MakeSessionId();
            IRunner<TResult>? runner;
            #if TRACE
            _logger?.LogTraceCreateRunner(session_id, trace_identifier);
            #endif
            Int32 runner_number = -1;
            Boolean use_session_lock = true;
            Object? runner_lock= RunnerManager.RunnerCreationLock;
            Int32 generation;
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
                String runner_session_key = SessionKey(ActiveSession.Id);
                generation = ActiveSession.Generation;
                String runner_key = RunnerKey(runner_session_key, runner_number, generation);
                RunnerId runner_id = (ActiveSession.Id, runner_number, generation);
                #if TRACE
                _logger?.LogTraceNewRunnerInfoRunner(session_id, generation, runner_number, trace_identifier);
                #endif
                try {
                    using (ICacheEntry new_entry = _memoryCache.CreateEntry(runner_key)) {
                        new_entry.SlidingExpiration=_runnerIdleTimeout;
                        new_entry.AbsoluteExpirationRelativeToNow=_maxLifetime;
                        IRunnerFactory<TRequest, TResult> factory = GetRunnerFactory<TRequest, TResult>(trace_identifier);
                        runner=factory.Create(Request, ActiveSession.SessionServices, runner_id, trace_identifier);
                        if (runner==null) {
                            _logger?.LogErrorCreateRunnerFailure(trace_identifier);
                            throw new InvalidOperationException("The factory failed to create a runner and returned null");
                        }
                        try {
                            _logger?.LogDebugCreateNewRunner(runner_id, trace_identifier);
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
                            RegisterRunnerInSession(Session, runner_session_key, runner_number, generation, typeof(TResult), trace_identifier);
                            try {

                                IChangeToken expiration_token = new CancellationChangeToken(ActiveSession.CompletionToken);
                                if (runner.CompletionToken.CanBeCanceled)
                                    expiration_token=new CompositeChangeToken(new IChangeToken[] { 
                                        expiration_token, 
                                        new CancellationChangeToken(runner.CompletionToken) 
                                    });
                                new_entry.ExpirationTokens.Add(expiration_token);
                                //An assignment to Value property should be the last one before new_entry.Dispose()
                                //to avoid adding bad entry to the cache by Dispose() 
                                new_entry.Value= runner;
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
                                UnregisterRunnerInSession(Session, runner_session_key, runner_number, generation);
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
                    _logger?.LogDebugCreateRunnerFailure(exception, session_id, trace_identifier);
                    throw;
                }

            }
            catch (Exception) {
                if(runner_number>=0) { 
                    #if TRACE
                    _logger?.LogTraceWaiveRunnerNumber(session_id, runner_number, trace_identifier);
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
            _logger?.LogTraceCreateRunnerExit(new RunnerId(ActiveSession, runner_number), trace_identifier);
            #endif
            return new KeyedRunner<TResult>() { Runner=runner, RunnerNumber=runner_number };
        }

        public IRunner? GetRunner(ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            Int32 RunnerNumber, 
            String? TraceIdentifier)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            String session_id = ActiveSession.Id;
            String runner_session_key = SessionKey(session_id);
            Int32 generation = ActiveSession.Generation;
            RunnerId runner_id = new RunnerId(ActiveSession, RunnerNumber);
            IRunner? result = null;
            #if TRACE
            _logger?.LogTraceGetRunner(runner_id, trace_identifier);
            #endif
            String? host_id;
            String runner_key = RunnerKey(runner_session_key, RunnerNumber, generation);
            host_id=Session.GetString(runner_key);
            if (host_id==null) {
                _logger?.LogInformationNoRunnerInSession(runner_id, trace_identifier);
                result= null;
            }
            else if (host_id==_hostId) {
                _logger?.LogDebugGetLocalRunnerFromCache(runner_id, trace_identifier);
                result=ExtractRunnerFromCache(Session, RunnerManager, ActiveSession, RunnerNumber, trace_identifier);
            }
            else {
                _logger?.LogDebugProcessRemoteRunner(runner_id, host_id, trace_identifier);
                result=MakeRemoteRunnerAsync(RunnerManager, host_id, ActiveSession, RunnerNumber, trace_identifier).GetAwaiter().GetResult();
            }
            #if TRACE
            _logger?.LogTraceGetRunnerExit(runner_id, result!=null,trace_identifier);
            #endif
            return result;
        }

        public async ValueTask<IRunner?> GetRunnerAsync(
            ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        )
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            String session_id = ActiveSession.Id;
            IRunner? result = null;
            RunnerId runner_id = new RunnerId(ActiveSession, RunnerNumber);
            Int32 generation = ActiveSession.Generation;
            #if TRACE
            _logger?.LogTraceGetRunnerAsync(runner_id, trace_identifier);
            #endif
            await Session.LoadAsync(Token);
            String runner_session_key = SessionKey(session_id);
            #if TRACE
            _logger?.LogTraceGetRunnerAsyncInfoRunner(runner_id, trace_identifier);
            #endif
            String? host_id;
            String runner_key = RunnerKey(runner_session_key, RunnerNumber, ActiveSession.Generation);
            host_id=Session.GetString(runner_key);
            if (host_id==null) {
                _logger?.LogInformationNoRunnerInSession(runner_id, trace_identifier);
                result= null;
            }
            else if (host_id==_hostId) {
                _logger?.LogDebugGetLocalRunnerFromCache(runner_id, trace_identifier);
                result=ExtractRunnerFromCache(Session, RunnerManager, ActiveSession, RunnerNumber, trace_identifier);
            }
            else {
                _logger?.LogDebugProcessRemoteRunner(runner_id, host_id, trace_identifier);
                #if TRACE
                _logger?.LogTraceAwaitForProxyCreation(runner_id, trace_identifier);
                #endif
                result= await MakeRemoteRunnerAsync(RunnerManager, host_id, ActiveSession, RunnerNumber, trace_identifier, Token);
            }
            #if TRACE
            _logger?.LogTraceGetRunnerAsyncExit(runner_id, result!=null, trace_identifier);
            #endif
            return result;
        }

        public Task TerminateSession(ISession Session, IActiveSession ActiveSession, IRunnerManager RunnerManager, String? TraceIdentifier)
        {
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            String session_id = ActiveSession.MakeSessionId();
            _logger?.LogTraceSessionTerminate(session_id, trace_identifier);
            #endif
            Int32 insession_generation = Session.GetInt32(SessionKey(ActiveSession.Id))??0;
            if(insession_generation!=-ActiveSession.Generation) {
                if(insession_generation==ActiveSession.Generation)
                    Session.SetInt32(SessionKey(ActiveSession.Id), -ActiveSession.Generation);
                else
                    _logger?.LogInfoInconsistentSessionTermination(ActiveSession.Generation, insession_generation, ActiveSession.Id, trace_identifier);
                DoTerminateSession(ActiveSession, trace_identifier);
            }
            else 
                #if TRACE
                _logger?.LogTraceSessionAlreadyTerminated(session_id, trace_identifier)
                #endif
                ;
            #if TRACE
            _logger?.LogTraceSessionTerminateExit(session_id, trace_identifier);
            #endif
            return ActiveSession.CleanupCompletionTask;
        }

        void DoTerminateSession(IActiveSession ActiveSession, String TraceIdentifier)
        {
            #if TRACE
            _logger?.LogTraceSessionDoTerminate(ActiveSession.Id, TraceIdentifier);
            #endif
            Object? dummy;
            String key= SessionKey(ActiveSession.Id);
            String session_id = ActiveSession.MakeSessionId();
            Boolean removed = false;
            Monitor.Enter(_creation_lock);
            try {
                #if TRACE
                _logger?.LogTraceSessionDoTerminateLockAcquired(session_id, TraceIdentifier);
                #endif
                if(_memoryCache.TryGetValue(key, out dummy) && Object.ReferenceEquals(dummy, ActiveSession)) {
                    //Try to free a cache slot synchronously, all disposing will be done by ActiveSessionEviction callback (asynchronously) 
#if TRACE
#endif
                    _logger?.LogTraceSessionDoTerminateViaEvict(session_id, TraceIdentifier);
                    _memoryCache.Remove(key);
                    removed=true;
                }
            }
            finally {
#if TRACE
#endif
                _logger?.LogTraceSessionDoTerminateLockReleased(session_id, TraceIdentifier);
                Monitor.Exit(_creation_lock);
            }
            if(!removed) {
                //Already have been evicted from cache. Nothing to do.
#if TRACE
#endif
                _logger?.LogTraceSessionDoTerminateDisposeEvicted(ActiveSession.Id, TraceIdentifier);
            }
            #if TRACE
            _logger?.LogTraceSessionDoTerminateExit(ActiveSession.Id, TraceIdentifier);
            #endif
        }

        public ActiveSessionStoreStats? GetCurrentStatistics()
        {
            return _trackStatistics?new ActiveSessionStoreStats() { 
                SessionCount=_currentSessionCount,
                RunnerCount=_currentRunnerCount,
                StoreSize=_currentStoreSize,
            } :null;
        }

        public IActiveSessionFeature AcquireFeatureObject(ISession? Session, String? TraceIdentier)
        {
            return new ActiveSessionFeature(this, Session, _loggerFactory?.CreateLogger(FEATURE_CATEGORY_NAME), TraceIdentier);
        }

        public void ReleaseFeatureObject(IActiveSessionFeature AnObject)
        {
            ActiveSessionFeature? feature = AnObject as ActiveSessionFeature;
            feature?.Clear();
        }
        #endregion

        #region PrivateMethods
        class RunnerManagerInfo
        {
            //To be filled in ActiveSessionEvictionCallback to contain RunnerManager.PerformRunnersCleanupAsync() result
            public Task? CleanupTask;  
        }

        void RunnerManagerCleanupWait(Object? State)
        {
            RunnerManagerInfo state =State as RunnerManagerInfo??throw new ArgumentNullException(nameof(State));
            if(state.CleanupTask==null) throw new NullReferenceException("State.CleanupTask is null.");
            state.CleanupTask!.Wait();
        }

        private void ActiveSessionEvictionCallback(object Key, object Value, EvictionReason Reason, object State)
        {
            SessionPostEvictionInfo session_info = (SessionPostEvictionInfo)State;
            String session_id = session_info.SessionId??UNKNOWN_SESSION_ID;
            #if TRACE
            _logger?.LogTraceSessionEvictionCallback(session_id);
            #endif
            Monitor.Enter(_creation_lock);
            try {
                #if TRACE
                _logger?.LogTraceSessionEvictionCallbackLocked(session_id);
                #endif
                _sessionKeys.Remove((String)Key);
            }
            finally {
                #if TRACE
                _logger?.LogTraceSessionEvictionCallbackUnlocked(session_id);
                #endif
                Monitor.Exit(_creation_lock);
            }
            ActiveSession active_session = (ActiveSession)Value;
            if (_trackStatistics) {
                Interlocked.Decrement(ref _currentSessionCount);
                Interlocked.Add(ref _currentStoreSize, -GetSessionSize());
            }
            #if TRACE
            _logger?.LogTraceAbortRunners(session_id);
            #endif
            session_info.RunnerManager.AbortAll(active_session); //Preliminary call before PerformRunnersCleanupAsync, may be omitted
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
            TimeoutLoggerInfo tl_info = new TimeoutLoggerInfo(active_session.MakeSessionId(), runners_cleanup_task);
            if(_cleanupLoggingTimeoutMs!=null)
                _cleanupLoggingTask=Task.WhenAny(active_session.CleanupCompletionTask, Task.Delay(_cleanupLoggingTimeoutMs.Value))
                    .ContinueWith(TimeoutLoggerBody, tl_info);
            #if TRACE
            _logger?.LogTraceSessionScopeToBeDisposed(session_id);
            #endif
            session_info?.SessionScope.Dispose();
            InitCacheExpiration();
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
            Boolean wait_succeded = state.CleanupTask.IsCompletedSuccessfully;
            #if TRACE
            _logger?.LogTraceActiveSessionEndWaitingForRunnersCompletion(state.ActiveSessionId, wait_succeded);
            #endif
        }

        private IRunnerFactory<TRequest, TResult> GetRunnerFactory<TRequest, TResult>(String TraceIdentifier)
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
            IRunnerFactory<TRequest, TResult>? factory = null;
            if (_factoryCache!=null) {
                if (_factoryCache.ContainsKey(key)) {
                    factory=(IRunnerFactory<TRequest, TResult>)_factoryCache[key];
                    #if TRACE
                    _logger?.LogTraceGetRunnerFactoryFromCache(request_type_name, result_type_name, TraceIdentifier);
                    #endif
                }
            }
            if (factory==null) {
                factory=_rootServiceProvider.GetRequiredService<IRunnerFactory<TRequest, TResult>>();
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
            RunnerId runner_id= new RunnerId(runner_info.ActiveSession, runner_info.Number);
            #if TRACE
            _logger?.LogTraceRunnerEvictionCallback(runner_id);
            #endif
            //Do not call UnregisterRunnerInSession here because the session is inaccessible here and may even be destroyed
            //One remove session variables of an unexisting runner while searching for it and cannot find it in the cache
            IRunner runner = runner_info.Runner;
            runner.Abort();
            runner_info.RunnerManager.UnregisterRunner(runner_info.ActiveSession, runner_info.Number);
            runner_info.RunnerManager.ReturnRunnerNumber(runner_info.ActiveSession, runner_info.Number);
            if (_trackStatistics) {
                Interlocked.Decrement(ref _currentRunnerCount);
                if(runner!=null) Interlocked.Add(ref _currentStoreSize, -GetRunnerSize(runner.GetType()));
            }
            #if TRACE
            _logger?.LogTraceRunnerEvictionCallbackExit(runner_id);
            #endif
        }

        //Methods to manage runner information in the session (ISession implementation)
        //
        //Key-value pairs to register/unregister
        // runner_key =_prefix+"_"+session_id+"#"+new RunnerKey(RunnerNumber,Generation).ToString()
        // т.е. runner_key =_prefix+"_"+session_id+"#"+{Generation}.ToString()+'-'+RunnerNumberToString()
        // $"{runner_key}" = _hostId
        // $"{runner_key}_Type" = ResultType
        private void RegisterRunnerInSession(ISession Session, String RunnerSessionKey, Int32 RunnerNumber, Int32 Generation, Type ResultType, String TraceIdentifier)
        {
            String runner_key = RunnerKey(RunnerSessionKey, RunnerNumber, Generation);
            #if TRACE
            _logger?.LogTraceRegisterRunnerInSession(Session.Id, runner_key, TraceIdentifier);
            #endif
            Session.SetString(runner_key, _hostId); 
            Session.SetString(runner_key+TYPE_KEY_PART, ResultType.FullName!);
            #if TRACE
            _logger?.LogTraceRegisterRunnerInSessionExit(Session.Id, runner_key, TraceIdentifier);
            #endif
        }

        private void UnregisterRunnerInSession(ISession Session, String RunnerSessionKey, Int32 RunnerNumber, Int32 Generation)
        {
            #if TRACE
            _logger?.LogTraceUnregisterRunnerInSession(Session?.Id??UNKNOWN_SESSION_ID, RunnerNumber);
            #endif
            if (Session?.IsAvailable??false) {
                #if TRACE
                _logger?.LogTracePerformUnregisteration(Session?.Id??UNKNOWN_SESSION_ID, RunnerNumber);
                #endif
                String runner_key = RunnerKey(RunnerSessionKey, RunnerNumber, Generation);
                Session!.Remove(runner_key);
                Session!.Remove(runner_key+TYPE_KEY_PART);
            }
            #if TRACE
            _logger?.LogTraceUnregisterRunnerInSessionExit(Session?.Id??UNKNOWN_SESSION_ID, RunnerNumber);
            #endif
        }

        void CleanupOutdatedRunnerVars(ISession Session, String ActiveSessionKey, int CurrentGeneration, 
            String SessionId, String TraceIdentifier)
        {
            #if TRACE
            _logger?.LogTraceCleanupOutdatedRunnerVars(SessionId, TraceIdentifier);
            #endif
            Int32 len = ActiveSessionKey.Length;
            Int16 char0 = Convert.ToInt16('0');
            List<String> runner_keys = Session.Keys
                .Where(key => key.StartsWith(ActiveSessionKey) && key.Length>len+1 && key[len]=='#' && Char.IsDigit(key[len+1]))
                .ToList();
            foreach(String key in runner_keys) {
                int gen = Convert.ToInt16(key[len+1])-char0;
                bool gen_ok=false;
                for(int i=len+2; i<key.Length; i++) {
                    if(key[i]=='-') {
                        gen_ok=true;
                        break;
                    }
                    else if(Char.IsDigit(key[i])) gen = gen*10+Convert.ToInt16(key[i])-char0;
                    else break;
                    if(gen>=CurrentGeneration) break;
                }
                if(gen_ok) {
                    #if TRACE
                    _logger?.LogTraceCleanupOutdatedRunnerVarsRemove(key, SessionId, TraceIdentifier);
                    #endif
                    Session.Remove(key);
                }
            }
            #if TRACE
            _logger?.LogTraceCleanupOutdatedRunnerVarsExit(SessionId, TraceIdentifier);
            #endif
        }

        private IRunner? ExtractRunnerFromCache(ISession Session, 
            IRunnerManager RunnerManager, IActiveSession ActiveSession, Int32 RunnerNumber, String TraceIdentifier)
        {
            Object? value_from_cache;
            IRunner? result;
            RunnerId runner_id = new RunnerId(ActiveSession, RunnerNumber);
            Int32 generation = ActiveSession.Generation;
            String runner_session_key = SessionKey(ActiveSession.Id);
            String runner_key = RunnerKey(runner_session_key, RunnerNumber, generation);
            #if TRACE
            _logger?.LogTraceExtractRunnerFromCache(runner_id, TraceIdentifier);
            #endif
            if (_memoryCache.TryGetValue(runner_key, out value_from_cache)) {
                #if TRACE
                _logger?.LogTraceReturnRunnerFromCache(runner_id, TraceIdentifier);
                #endif
                result=value_from_cache as IRunner;
                if(result==null) _logger?.LogWarningNotRunnerInCache(runner_id, TraceIdentifier);

            }
            else {
                #if TRACE
                _logger?.LogTraceNoExpectedRunnerInCache(runner_id, TraceIdentifier);
                #endif
                //Remove values connected with previosly evicted runner.
                //One could not do it from runner eviction callback because the session was not available there
                UnregisterRunnerInSession(Session, runner_session_key, RunnerNumber, generation);
                result = null;
            }
            #if TRACE
            _logger?.LogTraceExtractRunnerFromCacheExit(runner_id, TraceIdentifier);
            #endif
            return result;
        }


        private Task<IRunner?> MakeRemoteRunnerAsync(
#pragma warning disable IDE0060 // Remove unused parameter
            IRunnerManager RunnerManager,
            String HostId,
            IActiveSession ActiveSession,
            Int32 RunnerNumber,
            String TraceIdentifier,
            CancellationToken Token=default
#pragma warning restore IDE0060 // Remove unused parameter
        )
        {
            RunnerId runner_id = new RunnerId(ActiveSession, RunnerNumber);
            _logger?.LogWarningRemoteRunnerUnavailable(runner_id, TraceIdentifier);
            if (_throwOnRemoteRunner) {
                _logger?.LogErrorRemoteRunnerUnavailable(TraceIdentifier);
                throw new InvalidOperationException("Using remote runners is not allowed  configuration setting ThrowOnRemoteRunner");
            }
            return Task.FromResult<IRunner?>(null); //Just now I do not want to implement remote runner
            //Possible future implementation draft
            //String RunnerKey = this.RunnerKey(SessionKey(ActiveSession.Id), RunnerNumber, ActiveSession.Generation);
            //String? runner_type_name = RunnerManager.Session.GetString(RunnerKey+TYPE_KEY_PART);
            //if (runner_type_name==null) {
            //    // Log that runner has unknown type
            //    return null;
            //}
            //if (!s_ResultTypesDictionary.TryGetValue(runner_type_name, out runner_type)) {
            //    // Log that type is unregistered
            //    throw new InvalidOperationException(); //TODO Add message
            //}
            //                                                     //
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

        String RunnerKey(String SessionKey, Int32 RunnerNumber, Int32 Generation)
        {
            return $"{SessionKey}#{Generation}-{RunnerNumber}";
        }

        Int32 GetSessionSize()
        {
            return _activeSessionSize; 
        }

        Int32 GetRunnerSize(Type RunnerType)
        {
            return _runnerSize; //TODO(future) it's a stub
        }

        internal void InitCacheExpiration()
        {
            Object dummy;
            _memoryCache.TryGetValue("", out dummy);
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
            public IRunner Runner;

            public RunnerPostEvictionInfo(
                IRunnerManager RunnerManager,
                Int32 Number,
                Boolean UnregisterNumber,
                IActiveSession ActiveSession,
                IRunner Runner
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
