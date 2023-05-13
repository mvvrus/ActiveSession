using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //TODO Implement
    internal class ActiveSessionStore : IActiveSessionStore, IDisposable
    {
        readonly IMemoryCache _memoryCache;
        readonly IServiceProvider _rootServiceProvider;
        readonly string _prefix;
        readonly string _hostId;
        readonly bool _useOwnCache;
        readonly ILogger<ActiveSession> _logger;
        readonly TimeSpan _idleTimeout;
        readonly TimeSpan _maxLifetime;
        bool _disposed = false;

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
                    //TODO LogError Cannot create our own cache
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
            string key = _prefix + "_" + Session.Id;
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
                new_entry.Size = 1;
                new_entry.Value = result = new ActiveSession(_rootServiceProvider.CreateScope(), this, Session,_logger);
                PostEvictionCallbackRegistration end_activesession = new PostEvictionCallbackRegistration();
                end_activesession.EvictionCallback = EndActiveSessionCallback;
                end_activesession.State=new_entry.Value; 
                new_entry.PostEvictionCallbacks.Add(end_activesession);
                //TODO LogTrace Return new ActiveSession for the session
                throw new NotImplementedException();
            }
            return result;
        }

        private void EndActiveSessionCallback(object key, object value, EvictionReason reason, object state)
        {
            ActiveSession? active_session = value as ActiveSession;
            if (active_session != null)
            {
                //TODO
                //Evict all runners of the session
                //Dispose the session
                active_session.Dispose();
            }
            throw new NotImplementedException();
        }

        Task IActiveSessionStore.CommitAsync(ActiveSession Session, CancellationToken cancellationToken)
        {
            //TODO
            throw new NotImplementedException();
        }

        KeyedActiveSessionRunner<TResult> IActiveSessionStore.CreateRunner<TRequest, TResult>(ActiveSession Session, IServiceProvider Services)
        {
            //TODO
            throw new NotImplementedException();
        }

        public IActiveSessionRunner<TResult> FetchRunner<TResult>(ActiveSession Session, int KeyRequested)
        {
            //TODO
            throw new NotImplementedException();
        }
        private void CheckDisposed()
        {
            //TODO
            throw new NotImplementedException();
        }

    }
}
