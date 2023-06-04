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

        static readonly Dictionary<String,Type> s_ResultTypesDictionary = new Dictionary<String,Type>();

        internal static void RegisterTResult(Type TResult)
        {
            s_ResultTypesDictionary.TryAdd(TResult.FullName!, TResult);
        }

        public ActiveSessionStore(
            IMemoryCache Cache,
            IServiceProvider RootServiceProvider,
            IOptions<ActiveSessionOptions> Options,
            IOptions<SessionOptions> SessionOptions,
            ILoggerFactory? LoggerFactory
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

        public ActiveSession FetchOrCreate(ISession Session)
        {
            CheckDisposed();
#if TRACE
            _logger?.LogTraceFetchOrCreate();
#endif

            ActiveSession result;
            String key = SessionKey(Session);
            _logger?.LogDebugActiveSessionKeyToUse(key);
            if (_memoryCache.TryGetValue(key, out result))
            {
                _logger?.LogDebugFoundExistingActiveSession(key);
            }
            else
            {
                _logger?.LogDebugCreateNewActiveSession(key);
                ICacheEntry new_entry = _memoryCache.CreateEntry(key);
#if TRACE
                _logger?.LogTraceAddCacheEntry();
#endif
                new_entry.SlidingExpiration = _idleTimeout;
                new_entry.AbsoluteExpirationRelativeToNow = _maxLifetime;
                new_entry.Size = 1; //TODO Size from config?
                new_entry.Value = result = new ActiveSession(_rootServiceProvider.CreateScope(), this, Session, _logger);
#if TRACE
                _logger?.LogTraceCreateActiveSessionObject();
#endif
                PostEvictionCallbackRegistration end_activesession = new PostEvictionCallbackRegistration();
                end_activesession.EvictionCallback = EndActiveSessionCallback;
                new_entry.PostEvictionCallbacks.Add(end_activesession);
            }
#if TRACE
            _logger?.LogTraceFetchOrCreateExit();
#endif
            return result;
        }

        private void EndActiveSessionCallback(object key, object value, EvictionReason reason, object state)
        {
            ActiveSession? active_session = value as ActiveSession;
            if (active_session != null)
            {
                active_session.SignalCompletion(); //To evict all runners of the session
                active_session.Dispose(); 
            }
        }

        KeyedActiveSessionRunner<TResult> IActiveSessionStore.CreateRunner<TRequest, TResult>(
            ActiveSession RunnerSession
            , TRequest Request
        )
        {
            IActiveSessionRunnerFactory<TRequest, TResult> factory =
                _rootServiceProvider.GetRequiredService<IActiveSessionRunnerFactory<TRequest, TResult>>();
            IActiveSessionRunner<TResult>? runner = factory.Create(Request, RunnerSession.Services);
            if (runner==null) {
                //TODO Log the failure
                throw new InvalidOperationException("Cannot create the runner: the factory returned null");
            }
            Int32 runner_number = RunnerSession.GetNewRunnerNumber();

            //Store the runner in the cache
            String runner_key = RunnerKey(RunnerSession,runner_number);
            ICacheEntry new_entry = _memoryCache.CreateEntry(runner_key);
            new_entry.SlidingExpiration=_idleTimeout;
            new_entry.AbsoluteExpirationRelativeToNow=_maxLifetime;
            new_entry.Size=1; //TODO Size from config?
            new_entry.Value=_cacheAsTask ? Task.FromResult(runner) : runner;
            PostEvictionCallbackRegistration end_activesession = new PostEvictionCallbackRegistration();
            end_activesession.EvictionCallback=RunnerEvictionCallback;
            end_activesession.State =
                new RunnerPostEvictionInfo (RunnerSession, runner as IDisposable, runner_number, true);
            //TODO Add callback for the runner completion
            IChangeToken runner_completion_token = runner.GetCompletionToken();
            if(runner_completion_token.ActiveChangeCallbacks) {
                //TODO Create state info for the callback
                runner_completion_token.RegisterChangeCallback(
                    RunnerCompletionCallback, 
                    new RunnerPostCompletionInfo(runner,runner_key)
                ); 
            }
            new_entry.PostEvictionCallbacks.Add(end_activesession);
            RunnerSession.RegisterRunner(runner_number); 
            RegisterRunnerInSession(RunnerSession.Session, runner_key, typeof(TResult));
            return new KeyedActiveSessionRunner<TResult>() { Runner=runner, Key=runner_number };
        }

        private void RunnerCompletionCallback(Object obj)
        {
            RunnerPostCompletionInfo? runner_info = obj as RunnerPostCompletionInfo;
            if(runner_info == null) {
                //TODO Log warning?
                return;
            }
            ActiveSessionRunnerState state = runner_info.Runner.State;
            switch (state) {
                case ActiveSessionRunnerState.Aborted:
                    _memoryCache.Remove(runner_info.RunnerKey);
                    break;
                //TODO Add other completion states if required
                default: break;
            }
        }

        private void RegisterRunnerInSession(ISession Session, String RunnerKey, Type ResultType)
        {
            //Key-value pairs to register
            // ( {RuunerKey}=_prefix+"_"+Session.Id+"_"+RunnerNumber.ToString() )
            // {RunnerKey} = _hostId
            // {RunnerKey}+"_Type" = ResultType
            Session.SetString(RunnerKey, _hostId); //TODO Check for duplicates
            Session.SetString(RunnerKey+TYPE_KEY_PART, ResultType.FullName!);
        }

        private void RunnerEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            RunnerPostEvictionInfo runner_info = (RunnerPostEvictionInfo)state;
            if (runner_info.Disposable!=null)  runner_info.Disposable.Dispose();
            //TODO Unregister key-value pairs in ISession
            runner_info.RunnerSession.UnregisterRunner(runner_info.Number); 
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(ActiveSession RunnerSession, int KeyRequested)
        {
            String? host_id;
            String runner_key = RunnerKey(RunnerSession, KeyRequested);
            host_id=RunnerSession.Session.GetString(runner_key);
            if(host_id==null) {
                //TODO? log that runner does not exist
                return null;
            }
            if (host_id==_hostId) {
                return ExtractRunnerFromCache<TResult>(runner_key);
            }
            else {
                //TODO Trace remote runner
                return MakeRemoteRunnerAsync<TResult>(RunnerSession, runner_key).GetAwaiter().GetResult();
            }
        }

        private IActiveSessionRunner<TResult>? ExtractRunnerFromCache<TResult>(String runner_key)
        {
            Object? value_from_cache;
            if (_memoryCache.TryGetValue(runner_key, out value_from_cache)) {
                //TODO LogTrace Return existing ActiveSession for the session
                return _cacheAsTask ?
                    (value_from_cache as Task<IActiveSessionRunner<TResult>?>)?.Result :
                    value_from_cache as IActiveSessionRunner<TResult>;
            }
            else {
                //TODO Unregister key-value pairs in ISession
                return null;
            }
        }

        private Task<IActiveSessionRunner<TResult>?> MakeRemoteRunnerAsync<TResult>(
            ActiveSession RunnerSession, 
            String RunnerKey,
            CancellationToken Token=default
        )
        {
            if (_throwOnRemoteRunner) {
                //TODO Log that remote runner is not allowed?
                throw new InvalidOperationException("Using remote runners is not allowed");
            }
            return Task.FromResult<IActiveSessionRunner<TResult>?>(null); //Just now I do not want to implement remote runner
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
            //TODO
            throw new NotImplementedException();
        }

        String SessionKey(ISession Session)
        {
            return $"{_prefix}_{Session.Id}";
        }

        String RunnerKey(ActiveSession RunnerSession, Int32 RunnerNumber)
        {
            return $"{SessionKey(RunnerSession.Session)}_{RunnerNumber}";
        }

        public async ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            ActiveSession RunnerSession, 
            Int32 KeyRequested, 
            CancellationToken Token
        )
        {
            await RunnerSession.LoadAsync(Token);
            String? host_id;
            String runner_key = RunnerKey(RunnerSession, KeyRequested);
            host_id=RunnerSession.Session.GetString(runner_key);
            if (host_id==null) {
                //TODO? log that runner does not exist
                return null;
            }
            if (host_id==_hostId) {
                return ExtractRunnerFromCache<TResult>(runner_key);
            }
            else {
                //TODO Trace remote runner
                return await MakeRemoteRunnerAsync<TResult>(RunnerSession, runner_key, Token);
            }
        }

        internal class RunnerPostEvictionInfo
        {
            public ActiveSession RunnerSession;
            public IDisposable? Disposable;
            public Int32 Number;
            public Boolean UnregisterNumber;

            public RunnerPostEvictionInfo(ActiveSession RunnerSession, IDisposable? Disposable, Int32 Number, Boolean UnregisterNumber)
            {
                this.RunnerSession=RunnerSession;
                this.Disposable=Disposable;
                this.Number=Number;
                this.UnregisterNumber=UnregisterNumber;
            }
        }

        private class RunnerPostCompletionInfo
        {
            internal String RunnerKey;
            internal IActiveSessionRunner Runner;

            public RunnerPostCompletionInfo(IActiveSessionRunner Runner, String RunnerKey)
            {
                this.Runner=Runner;
                this.RunnerKey=RunnerKey;
            }
        }
    }
}
