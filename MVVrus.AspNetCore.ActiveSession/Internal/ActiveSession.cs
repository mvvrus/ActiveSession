namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSession : IActiveSession, IDisposable
    {
        readonly IServiceProvider _services;
        readonly IActiveSessionStore _store;
        readonly IServiceScope _scope;
        readonly ISession? _session;
        readonly ILogger<ActiveSession> _logger;
        bool _disposed;
        bool _isFresh = true;
        readonly CancellationTokenSource _completionTokenSource;

        public CancellationToken CompletionToken { get {return _completionTokenSource.Token; } }

        public void SignalCompletion() { 
            _completionTokenSource.Cancel();
        }

        public ActiveSession(
            IServiceScope SessionScope
            , IActiveSessionStore Store
            , ISession? Session
            , ILogger<ActiveSession> Logger
        )
        {
            _scope = SessionScope;
            _services = _scope.ServiceProvider;
            _store = Store;
            _session = Session;
            _logger=Logger;
            _completionTokenSource = new CancellationTokenSource();
            //TODO LogTrace?
        }

        public KeyedActiveSessionRunner<TResult> GetRunner<TRequest, TResult>(TRequest Request)
        {
            _isFresh = false;
            //TODO LogTrace?
            return _store.CreateRunner<TRequest, TResult>(this, _services);
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey)
        {
            IActiveSessionRunner<TResult>? fetched = _store.FetchRunner<TResult>(this, RequestedKey);
            _isFresh = false;
            if (fetched != null)
            {
                if (typeof(IActiveSessionRunner<TResult>).IsAssignableFrom(fetched.GetType()))
                    return fetched as IActiveSessionRunner<TResult>;
                else //TODO Implement error logging
                    throw new InvalidCastException();
            }
            else return null;
        }

        public bool IsAvailable { get { return _session?.IsAvailable ?? false; } }

        public bool IsFresh => _isFresh;

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            //TODO
            throw new NotImplementedException();
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            //TODO Is needed at all?
            return _store.CommitAsync(this, cancellationToken);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _scope.Dispose();
        }
    }
}
