using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSession : IActiveSession
    {
        readonly IActiveSessionStore _store;
        readonly IServiceScope _scope;
        readonly ILogger? _logger;
        readonly String _sessionId;
        Int32 _disposed=0;
        bool _isFresh = true;
        readonly IRunnerManager _runnerManager;

        //Properties used in tests
        internal IRunnerManager RunnerManager { get { return _runnerManager; } }

        public ActiveSession(
            IRunnerManager RunnerManager
            , IServiceScope SessionScope
            , IActiveSessionStore Store
            , ISession Session
            , ILogger? Logger
            , String? TraceIdentifier = null
        )
        {
            if (Session is null) throw new ArgumentNullException(nameof(Session));
            _logger=Logger;
            _sessionId=Session.Id;
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionConstructor(_sessionId, trace_identifier);
            #endif
            _scope = SessionScope??throw new ArgumentNullException(nameof(SessionScope));
            _runnerManager = RunnerManager??throw new ArgumentNullException(nameof(RunnerManager));
            _store = Store??throw new ArgumentNullException(nameof(Store));
            #if TRACE
            _logger?.LogTraceActiveSessionConstructorExit(trace_identifier);
            #endif
        }

        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionCreateRunner(_sessionId, trace_identifier);
            #endif
            KeyedActiveSessionRunner<TResult> created = _store.CreateRunner<TRequest, TResult>(Context.Session, _runnerManager, Request, trace_identifier);
            _isFresh = false;
            #if TRACE
            _logger?.LogTraceCreateActiveSessionCreateRunnerExit(trace_identifier);
            #endif
            return created; 
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunner(_sessionId, trace_identifier);
            #endif
            IActiveSessionRunner<TResult>? fetched = _store.GetRunner<TResult>(Context.Session,_runnerManager, RequestedKey, trace_identifier);
            _isFresh = false;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerExit(trace_identifier);
            #endif
            return fetched;
        }

        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, HttpContext Context, CancellationToken Token)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsync(_sessionId, trace_identifier);
            #endif
            ValueTask<IActiveSessionRunner<TResult>?> fetched = _store.GetRunnerAsync<TResult>(Context.Session, _runnerManager, RequestedKey, trace_identifier, Token);
            _isFresh=false;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsyncExit(trace_identifier);
            #endif
            return fetched;
        }

        public bool IsAvailable { get { return true; } }

        public bool IsFresh => _isFresh;

        public IServiceProvider SessionServices { get { return _scope.ServiceProvider; } }

        public String Id { get { return _sessionId; } }

        public CancellationToken CompletionToken { get { return _runnerManager.SessionCompletionToken; } }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1)!=0) return; 
            DisposeAsyncCore().GetAwaiter().GetResult();
        }


        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1)!=0) return ValueTask.CompletedTask;
            else return DisposeAsyncCore();
        }

        ValueTask DisposeAsyncCore()
        {
            #if TRACE
            _logger?.LogTraceActiveSessionDispose(_sessionId);
            #endif
            return new ValueTask(Task.Run(CompleteDispose));
        }

        public Boolean HasAbandonedRunners { get; private set; }

        private void CompleteDispose()
        {
            //TODO to be removed in conjucntion with DisposeAsync & DisposeAsyncCore
        }

        private void CheckDisposed()
        {
            if(Volatile.Read(ref _disposed)!=0)  throw new ObjectDisposedException(this.GetType().FullName!);
        }

        //Stuff used just for testing
        internal void SetDisposedForTests()
        {
            _disposed=1;
        }

        internal Boolean Disposed { get { return _disposed!=0; }}
    }
}
