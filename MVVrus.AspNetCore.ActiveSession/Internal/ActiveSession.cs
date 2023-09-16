using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSession : IActiveSession, IDisposable, IRunnerManager
    {
        readonly IServiceProvider _services;
        readonly IActiveSessionStore _store;
        readonly IServiceScope _scope;
        readonly ILogger? _logger;
        readonly String _sessionId;
        bool _disposed;
        bool _isFresh = true;
        Int32 _newRunnerNumber;
#pragma warning disable CS0169 // The field 'ActiveSession._keyMap' is never used
#pragma warning disable IDE0051 // Remove unused private members
        readonly Byte[] ? _keyMap;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0169 // The field 'ActiveSession._keyMap' is never used
#pragma warning disable IDE0052 // Remove unread private members
        readonly Int32 _minRunnerNumber, _maxRunnerNumber;
#pragma warning restore IDE0052 // Remove unread private members
        readonly CancellationTokenSource _completionTokenSource;
        readonly CountdownEvent _runnersCounter;

#if DEBUG
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ActiveSession() { }/*for mocking*/
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#endif

        public ActiveSession(
            IServiceScope SessionScope
            , IActiveSessionStore Store
            , ISession Session
            , ILogger? Logger
            , String? TraceIdentifier = null
            , Int32 MinRunnerNumber = 0
            , Int32 MaxRunnerNumber = Int32.MaxValue
        )
        {
            _logger=Logger;
            _sessionId=Session.Id;
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionConstructor(_sessionId, trace_identifier);
            #endif
            _scope= SessionScope;
            _services = _scope.ServiceProvider;
            _store = Store;
            _completionTokenSource = new CancellationTokenSource();
            _runnersCounter=new CountdownEvent(1);
            _newRunnerNumber=_minRunnerNumber=MinRunnerNumber;
            _maxRunnerNumber=MaxRunnerNumber;
            if (MaxRunnerNumber!=Int32.MaxValue) { 
                //TODO Implement runner number reusage
            }
            #if TRACE
            _logger?.LogTraceActiveSessionConstructorExit(trace_identifier);
            #endif
            //TODO LogTrace?
        }

        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionCreateRunner(_sessionId, trace_identifier);
            #endif
            KeyedActiveSessionRunner<TResult> created = _store.CreateRunner<TRequest, TResult>(Context.Session, this, Request, trace_identifier);
            _isFresh = false;
            //TODO LogTrace?
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
            IActiveSessionRunner<TResult>? fetched = _store.GetRunner<TResult>(Context.Session,this, RequestedKey, trace_identifier);
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
            ValueTask<IActiveSessionRunner<TResult>?> fetched = _store.GetRunnerAsync<TResult>(Context.Session, this, RequestedKey, trace_identifier, Token);
            _isFresh=false;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsyncExit(trace_identifier);
            #endif
            return fetched;
        }

        public bool IsAvailable { get { return true; } }

        public bool IsFresh => _isFresh;

        public IServiceProvider SessionServices { get { return _services; } }

        public String Id { get { return _sessionId; } }

        public CancellationToken CompletionToken { get { return _completionTokenSource.Token; } }

        public void SignalCompletion()
        {
            if (_disposed) return;
            SignalCompletionInternal();
        }

        void SignalCompletionInternal()
        {
            #if TRACE
            _logger?.LogTraceActiveSessionSignalCompletion(_sessionId);
            #endif
            _completionTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (_disposed) return;
            #if TRACE
            _logger?.LogTraceActiveSessionDispose(_sessionId);
            #endif
            _disposed=true;
            SignalCompletionInternal(); //Just in case, usually this is called by post-eviction proc.
            Task.Run(CompleteDispose);
        }

        const Int32 RUNNERS_TIMEOUT_MSEC= 10000;

        private void CompleteDispose()
        {
            #if TRACE
            _logger?.LogTraceActiveSessionCompleteDispose(_sessionId);
            #endif
            _runnersCounter.Signal();
            #if TRACE
            _logger?.LogTraceActiveSessionCompleteDisposeWaitForRunners(_sessionId);
            #endif
            Boolean wait_succeded=_runnersCounter.Wait(RUNNERS_TIMEOUT_MSEC); //Wait for disposing all runners
            #if TRACE
            _logger?.LogTraceActiveSessionEndWaitingForRunnersCompletion(_sessionId, wait_succeded);
            #endif
            _scope.Dispose();
            _completionTokenSource.Dispose();
            #if TRACE
            _logger?.LogTraceActiveSessionCompleteDisposeExit(_sessionId);
            #endif
        }

        private void CheckDisposed()
        {
            if(_disposed)  throw new ObjectDisposedException(this.GetType().FullName!);
        }

        public void RegisterRunner (int RunnerNumber)
        {
            #if TRACE
            _logger?.LogTraceRegisterRunnerNumber(_sessionId, RunnerNumber);
            #endif
            _runnersCounter.AddCount();    
        }

        public void UnregisterRunner(int RunnerNumber)
        {
            #if TRACE
            _logger?.LogTraceUnregisterRunnerNumber(_sessionId, RunnerNumber);
            #endif
            _runnersCounter.Signal();
            ReturnRunnerNumber(RunnerNumber);
        }

        public void ReturnRunnerNumber(Int32 RunnerNumber)
        {
            #if TRACE
            _logger?.LogTraceReturnRunnerNumber(_sessionId, RunnerNumber);
            #endif
            //Do nothing until RunnerNumber reusage is implemented
        }


        public Int32 GetNewRunnerNumber(String? TraceIdentifier=null)
        {
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceGetNewRunnerNumber(_sessionId, trace_identifier);
            #endif
            if (_newRunnerNumber>_maxRunnerNumber) {
                _logger?.LogErrorCannotAllocateRunnerNumber(_sessionId, trace_identifier);
                throw new InvalidOperationException("Cannot acquire a new runner number");
            }
            int result = _newRunnerNumber++;
            #if TRACE
            _logger?.LogTraceGetNewRunnerNumberExit(_sessionId, result, trace_identifier);
            #endif
            return result;
        }

        public IServiceProvider Services { get { return _services; } }

        public Object RunnerCreationLock { get; init; } = new Object();
    }
}
