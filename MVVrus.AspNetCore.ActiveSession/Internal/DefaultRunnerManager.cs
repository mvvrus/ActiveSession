using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class DefaultRunnerManager : IRunnerManager, IDisposable
    {
        readonly CountdownEvent _runnersCounter;
        readonly String _sessionId;
        readonly ILogger? _logger;
        readonly IServiceProvider _services;
        Int32 _newRunnerNumber;
#pragma warning disable CS0169 // The field 'ActiveSession._keyMap' is never used
#pragma warning disable IDE0051 // Remove unused private members
        readonly Byte[]? _keyMap;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0169 // The field 'ActiveSession._keyMap' is never used
#pragma warning disable IDE0052 // Remove unread private members
        readonly Int32 _minRunnerNumber, _maxRunnerNumber;
#pragma warning restore IDE0052 // Remove unread private members
        readonly CancellationTokenSource _completionTokenSource;

        //For tests
        internal CountdownEvent RunnersCounter => _runnersCounter;

        public DefaultRunnerManager(
            String sessionId
            , ILogger? logger
            , IServiceProvider Services
            , Int32 MinRunnerNumber = 0
            , Int32 MaxRunnerNumber = Int32.MaxValue
        )
        {
            _runnersCounter=new CountdownEvent(1);
            _sessionId=sessionId;
            _logger=logger;
            _services=Services;
            _newRunnerNumber=_minRunnerNumber=MinRunnerNumber;
            _maxRunnerNumber=MaxRunnerNumber;
            _completionTokenSource=new CancellationTokenSource();
            if (MaxRunnerNumber!=Int32.MaxValue) {
                //TODO Implement runner number reusage
            }
        }

        public void RegisterRunner(int RunnerNumber)
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
        }

        public void ReturnRunnerNumber(Int32 RunnerNumber)
        {
            #if TRACE
            _logger?.LogTraceReturnRunnerNumber(_sessionId, RunnerNumber);
            #endif
            //Do nothing until RunnerNumber reusage is implemented
        }


        public Int32 GetNewRunnerNumber(String? TraceIdentifier = null)
        {
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceGetNewRunnerNumber(_sessionId, trace_identifier);
            #endif
            if (_newRunnerNumber>=_maxRunnerNumber) {
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

        public CancellationToken SessionCompletionToken { get { return _completionTokenSource.Token; } }

        public Boolean WaitForRunners(Int32 Timeout)
        {
            _runnersCounter.Signal();
            _completionTokenSource.Cancel();
            Boolean wait_succeded = _runnersCounter.Wait(Timeout);
            return wait_succeded;
        }

        public void Dispose()
        {
            _runnersCounter?.Dispose();
            _completionTokenSource.Dispose();
        }

    }
}
