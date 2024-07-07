using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSession : IActiveSession, IDisposable
    {
        readonly IActiveSessionStore _store;
        readonly IServiceScope _scope;
        readonly ILogger? _logger;
        readonly String _sessionId;
        Int32 _disposed = 0;
        bool _isFresh = true;
        readonly IRunnerManager _runnerManager;
        readonly CancellationTokenSource _cts;
        readonly IDictionary<String, Object> _properties;

        //Properties used in tests
        internal IRunnerManager RunnerManager { get { return _runnerManager; } }

        public ActiveSession(
            IRunnerManager RunnerManager
            , IServiceScope SessionScope
            , IActiveSessionStore Store
            , String SessionId
            , ILogger? Logger
            , Int32 Generation
            , Task? CleanupCompletionTask = null
            , String? TraceIdentifier = null
        )
        {
            if (SessionId is null) throw new ArgumentNullException(nameof(SessionId));
            _logger=Logger;
            _sessionId=SessionId;
            this.Generation=Generation;
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionConstructor(_sessionId, trace_identifier);
            #endif
            _scope=SessionScope??throw new ArgumentNullException(nameof(SessionScope));
            _runnerManager=RunnerManager??throw new ArgumentNullException(nameof(RunnerManager));
            _store=Store??throw new ArgumentNullException(nameof(Store));
            _cts=new CancellationTokenSource();
            CompletionToken=_cts.Token;
            this.CleanupCompletionTask=CleanupCompletionTask??Task.CompletedTask;
            _properties= new SortedList<String, Object>();
            #if TRACE
            _logger?.LogTraceActiveSessionConstructorExit(trace_identifier);
            #endif
        }

        public KeyedRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionCreateRunner(_sessionId, trace_identifier);
            #endif
            KeyedRunner<TResult> created = _store.CreateRunner<TRequest, TResult>(Context.Session,
                this,
                _runnerManager,
                Request,
                trace_identifier);
            _isFresh=false;
            #if TRACE
            _logger?.LogTraceCreateActiveSessionCreateRunnerExit(trace_identifier);
            #endif
            return created;
        }

        public IRunner<TResult>? GetRunner<TResult>(int RequestedKey, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunner(_sessionId, trace_identifier);
            #endif
            IRunner<TResult>? fetched = _store.GetRunner<TResult>(Context.Session, this, _runnerManager, RequestedKey, trace_identifier);
            _isFresh=false;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerExit(trace_identifier);
            #endif
            return fetched;
        }

        public Task<IRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, HttpContext Context, CancellationToken Token)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsync(_sessionId, trace_identifier);
            #endif
            Task<IRunner<TResult>?> fetched = _store.GetRunnerAsync<TResult>(Context.Session, this, _runnerManager, RequestedKey, trace_identifier, Token);
            _isFresh=false;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsyncExit(trace_identifier);
            #endif
            return fetched;
        }

        public Task Terminate(HttpContext Context)
        {
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionTerminateCalled(_sessionId, trace_identifier);
            #endif
            return _store.TerminateSession(Context.Session, this, RunnerManager, trace_identifier);
        }

        public bool IsAvailable { get { return true; } }

        public bool IsFresh => _isFresh;

        public bool IsIdle => _isFresh; //TODO(future) Make real implementation instead of this stub

        public IServiceProvider SessionServices { get { return _scope.ServiceProvider; } }

        public String Id { get { return _sessionId; } }

        public CancellationToken CompletionToken { get; private set; }

        public Task CleanupCompletionTask { get; private set; }

        public IDictionary<String, Object> Properties { get=>_properties; }


        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1)!=0) return;
            #if TRACE
            _logger?.LogTraceActiveSessionDispose(_sessionId);
            #endif
            _cts.Cancel();
            _cts.Dispose();
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

        //TODO(future) Make real implementation instead of this stub
        public ValueTask<Boolean> WaitUntilIdle(Boolean AbortAll, TimeSpan Timeout)
        {
            return new ValueTask<bool>(IsIdle);
        }

        public Task? TrackRunnerCleanup(Int32 RunnerNumber)
        {
            return _runnerManager.GetRunnerCleanupTrackingTask(this, RunnerNumber);
        }

        internal Boolean Disposed { get { return _disposed!=0; }}

        public Int32 Generation { get; init; }
    }
}
