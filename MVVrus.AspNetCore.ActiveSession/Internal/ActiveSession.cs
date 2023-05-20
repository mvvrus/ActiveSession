namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSession : IActiveSession, IDisposable
    {
        readonly IServiceProvider _services;
        readonly IActiveSessionStore _store;
        readonly IServiceScope _scope;
        readonly ISession _session;
        readonly ILogger<ActiveSession> _logger; //TODO - extract from scoped SP?
        bool _disposed;
        bool _isFresh = true;
        private Int32 _lastKey;
        readonly CancellationTokenSource _completionTokenSource;
        readonly CountdownEvent _runnersCounter;

        public CancellationToken CompletionToken { get {return _completionTokenSource.Token; } }

        public void SignalCompletion() { 
            _completionTokenSource.Cancel();
        }

        public ActiveSession(
            IServiceScope SessionScope
            , IActiveSessionStore Store
            , ISession Session
            , ILogger<ActiveSession> Logger
        )
        {
            _scope = SessionScope;
            _services = _scope.ServiceProvider;
            _store = Store;
            _session = Session;
            _logger=Logger;
            _completionTokenSource = new CancellationTokenSource();
            _runnersCounter=new CountdownEvent(1);
            //TODO LogTrace?
        }

        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request)
        {
            _isFresh = false;
            //TODO LogTrace?
            return _store.CreateRunner<TRequest, TResult>(this, Request);
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey)
        {
            IActiveSessionRunner<TResult>? fetched = _store.GetRunner<TResult>(this, RequestedKey);
            _isFresh = false;
            return fetched;
        }

        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, CancellationToken Token)
        {
            ValueTask<IActiveSessionRunner<TResult>?> fetched = _store.GetRunnerAsync<TResult>(this, RequestedKey, Token);
            _isFresh=false;
            return fetched;
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
            SignalCompletion(); //Just in case, usually this is called by post-eviction proc.
            Task.Run(CompleteDispose);
        }

        const Int32 RUNNERS_TIMEOUT_MSEC= 10000;

        private void CompleteDispose()
        {
            _runnersCounter.Signal();
            _runnersCounter.Wait(RUNNERS_TIMEOUT_MSEC); //Wait for disposing all runners
            _scope.Dispose();
            _completionTokenSource.Dispose();
            Dispose();
        }

        public void RegisterRunner ()
        {
            _runnersCounter.AddCount();    
        }
        public void UnregisterRunner()
        {
            _runnersCounter.Signal();
        }

        public Int32 GetNewKey()
        {
            return ++_lastKey;
        }

        public IServiceProvider Services { get { return _services; } }

        public ISession Session { get { return _session; } }
    }
}
