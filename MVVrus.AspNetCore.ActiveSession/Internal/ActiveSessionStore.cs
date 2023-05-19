using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //TODO Implement
    internal class ActiveSessionStore : IActiveSessionStore, IDisposable
    {
        const string TYPE_KEY_PART = "_Type";

        readonly IMemoryCache _memoryCache;
        readonly IServiceProvider _rootServiceProvider;
        readonly string _prefix;
        readonly string _hostId;
        readonly bool _useOwnCache;
        readonly ILogger<ActiveSession> _logger;
        readonly TimeSpan _idleTimeout;
        readonly TimeSpan _maxLifetime;
        readonly Boolean _throwOnRemoteRunner;
        bool _disposed = false;
        static readonly Dictionary<String,Type> s_ResultTypesDictionary = new Dictionary<String,Type>();

        public static void RegisterTResult(Type TResult)
        {
            s_ResultTypesDictionary.TryAdd(TResult.FullName!, TResult);
        }

        public ActiveSessionStore(
            IMemoryCache Cache,
            IServiceProvider RootServiceProvider,
            IOptions<ActiveSessionOptions> Options,
            IOptions<SessionOptions> SessionOptions,
            ILoggerFactory LoggerFactory
        )
        {
            _logger = LoggerFactory.CreateLogger<ActiveSession>();
            _rootServiceProvider = RootServiceProvider;
            ActiveSessionOptions options = Options.Value;
            //TODO LogTrace options
            _useOwnCache = options.UseOwnCache ?? false;
            if (_useOwnCache)
            {
                //TODO LogTrace options.OwnCacheOptions
                try
                {
                    _memoryCache = new MemoryCache(options.OwnCacheOptions);
                }
                catch
                {
                    //TODO LogError Cannot create our own cache ?
                    throw;
                }
            }
            else
            {
                //TODO LogTrace Using shared cache
                //TODO Check that shared cache exists 
                _memoryCache = Cache;
            }
            _hostId = options.HostId!;
            _prefix = options.Prefix ?? "";
            _idleTimeout = SessionOptions.Value.IdleTimeout;
            _maxLifetime = options.MaxLifetime ?? ActiveSessionOptions.DEFAULT_MAX_LIFETIME;
            _throwOnRemoteRunner=options.ThrowOnRemoteRunner;
            //TODO Implement remaining logic, if any
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_useOwnCache) _memoryCache.Dispose();
            GC.SuppressFinalize(this);
        }

        public ActiveSession FetchOrCreate(ISession Session)
        {
            CheckDisposed();

            ActiveSession result;
            String key = SessionKey(Session);
            if (_memoryCache.TryGetValue(key, out result))
            {
                //TODO LogTrace Return existing ActiveSession for the session
            }
            else
            {
                //Create the new ActiveSession
                ICacheEntry new_entry = _memoryCache.CreateEntry(key);
                new_entry.SlidingExpiration = _idleTimeout;
                new_entry.AbsoluteExpirationRelativeToNow = _maxLifetime;
                new_entry.Size = 1; //TODO Size from config?
                new_entry.Value = result = new ActiveSession(_rootServiceProvider.CreateScope(), this, Session, _logger);
                PostEvictionCallbackRegistration end_activesession = new PostEvictionCallbackRegistration();
                end_activesession.EvictionCallback = EndActiveSessionCallback;
                new_entry.PostEvictionCallbacks.Add(end_activesession);
                //TODO LogTrace Return new ActiveSession for the session
            }
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

        Task IActiveSessionStore.CommitAsync(ActiveSession RunnerSession, CancellationToken cancellationToken)
        {
            //TODO
            throw new NotImplementedException();
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
            Int32 runner_number = RunnerSession.GetNewKey();

            //Store the runner in the cache
            String runner_key = RunnerKey(RunnerSession,runner_number);
            ICacheEntry new_entry = _memoryCache.CreateEntry(runner_key);
            new_entry.SlidingExpiration=_idleTimeout;
            new_entry.AbsoluteExpirationRelativeToNow=_maxLifetime;
            new_entry.Size=1; //TODO Size from config?
            new_entry.Value=runner;
            PostEvictionCallbackRegistration end_activesession = new PostEvictionCallbackRegistration();
            end_activesession.EvictionCallback=RunnerEvictionCallback;
            end_activesession.State =
                new ActiveSessionRunnerInfo (RunnerSession, runner as IDisposable, runner_number, true);
            new_entry.PostEvictionCallbacks.Add(end_activesession);
            RunnerSession.RegisterRunner(); //TODO Register the runner with the number
            RegisterRunnerInSession(RunnerSession.Session, runner_key, typeof(TResult));
            return new KeyedActiveSessionRunner<TResult>() { Runner=runner, Key=runner_number };
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
            ActiveSessionRunnerInfo runner_info = (ActiveSessionRunnerInfo)state;
            IDisposable? runner_disposable = value as IDisposable;
            if (runner_disposable!=null) runner_disposable.Dispose();
            //TODO Unregister key-value pairs in ISession
            runner_info.Session.UnregisterRunner(); //TODO Unregister the runner with the number
        }

        public IActiveSessionRunner<TResult>? FetchRunner<TResult>(ActiveSession RunnerSession, int KeyRequested)
        {
            String runner_key = RunnerKey(RunnerSession, KeyRequested);
            String? host_id;
            host_id=RunnerSession.Session.GetString(runner_key);
            if(host_id==null) {
                //TODO? log that runner does not exist
                return null;
            }
            if (host_id==_hostId) {
                //TODO Search local runner object in the cache
                throw new NotImplementedException();
            }
            else {
                //TODO Trace remote runner
                return MakeRemoteRunner<TResult>(RunnerSession, runner_key);
            }
        }

        private IActiveSessionRunner<TResult>? MakeRemoteRunner<TResult>(ActiveSession RunnerSession, String RunnerKey)
        {
            if (_throwOnRemoteRunner) {
                //TODO Log that remote runner is not allowed?
                throw new InvalidOperationException("Using remote runners is not allowed");
            }
            return null; //Just now I do not want to implement remote runner
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

    }
}
