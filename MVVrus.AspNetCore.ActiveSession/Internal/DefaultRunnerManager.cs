using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class DefaultRunnerManager : IRunnerManager, IDisposable
    {
        readonly CountdownEvent _runnersCounter;
        readonly ILogger? _logger;
        readonly IServiceProvider _services;
        IActiveSession? _sessionKey = null;
        Int32 _newRunnerNumber;
#pragma warning disable CS0169 // The field 'ActiveSession._keyMap' is never used
#pragma warning disable IDE0051 // Remove unused private members
        readonly Byte[]? _keyMap;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0169 // The field 'ActiveSession._keyMap' is never used
#pragma warning disable IDE0052 // Remove unread private members
        readonly Int32 _minRunnerNumber, _maxRunnerNumber;
#pragma warning restore IDE0052 // Remove unread private members

        //For tests
        internal CountdownEvent RunnersCounter => _runnersCounter;

        public DefaultRunnerManager(
            ILogger? logger
            , IServiceProvider Services
            , Int32 MinRunnerNumber = 0
            , Int32 MaxRunnerNumber = Int32.MaxValue
        )
        {
            _runnersCounter=new CountdownEvent(1);
            _logger=logger;
            _services=Services;
            _newRunnerNumber=_minRunnerNumber=MinRunnerNumber;
            _maxRunnerNumber=MaxRunnerNumber;
            if (MaxRunnerNumber!=Int32.MaxValue) {
                //TODO Implement runner number reusage
            }
        }

        void CheckSession(IActiveSession SessionKey)
        {
            if (!ReferenceEquals(_sessionKey, SessionKey))
                throw new InvalidOperationException("DefaultRunnermanager can serve runners from one session only/");
        }

        public void RegisterSession(IActiveSession SessionKey)
        {
            if (_sessionKey==null) _sessionKey=SessionKey;
            else CheckSession(SessionKey);
        }


        public void RegisterRunner(IActiveSession SessionKey, int RunnerNumber, IActiveSessionRunner Runner, Type ResultType)
        {
            CheckSession(SessionKey);
            #if TRACE
            _logger?.LogTraceRegisterRunnerNumber(SessionKey.Id, RunnerNumber);
            #endif
            _runnersCounter.AddCount();
        }

        public void UnregisterRunner(IActiveSession SessionKey, int RunnerNumber)
        {
            CheckSession(SessionKey);
            #if TRACE
            _logger?.LogTraceUnregisterRunnerNumber(SessionKey.Id, RunnerNumber);
            #endif
            _runnersCounter.Signal();
        }

        public void ReturnRunnerNumber(IActiveSession SessionKey, Int32 RunnerNumber)
        {
            CheckSession(SessionKey);
            #if TRACE
            _logger?.LogTraceReturnRunnerNumber(SessionKey.Id, RunnerNumber);
            #endif
            //Do nothing until RunnerNumber reusage is implemented
        }


        public Int32 GetNewRunnerNumber(IActiveSession SessionKey, String? TraceIdentifier = null)
        {
            CheckSession(SessionKey);
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceGetNewRunnerNumber(SessionKey.Id, trace_identifier);
            #endif
            if (_newRunnerNumber>=_maxRunnerNumber) {
                _logger?.LogErrorCannotAllocateRunnerNumber(SessionKey.Id, trace_identifier);
                throw new InvalidOperationException("Cannot acquire a new runner number");
            }
            int result = _newRunnerNumber++;
            #if TRACE
            _logger?.LogTraceGetNewRunnerNumberExit(SessionKey.Id, result, trace_identifier);
            #endif
            return result;
        }

        public Object RunnerCreationLock { get; init; } = new Object();

        public Boolean WaitForRunners(IActiveSession SessionKey, Int32 Timeout)
        {
            //TODO LogTrace
            _runnersCounter.Signal();
            Boolean wait_succeded = _runnersCounter.Wait(Timeout);
            return wait_succeded;
        }

        public void Dispose()
        {
            _runnersCounter?.Dispose();
        }

    }
}
