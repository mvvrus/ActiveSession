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
        readonly Dictionary<int, RunnerInfo> _runners;
        readonly HashSet<Task> _runningDisposeTasks;
        readonly int? _cleanupLoggingTimeoutMs;
        internal Task? _cleanupLoggingTask;  //For unit tests only, no need to take into account any parallelism
        Boolean _cleanup_mark;
        Int32 _disposed;

        //For tests
        internal CountdownEvent RunnersCounter => _runnersCounter;

        public DefaultRunnerManager(
            ILogger? logger
            , IServiceProvider Services
            , int? CleanupLoggingTimeoutMs=null
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
                //TODO (future) Implement runner number reusage
            }
            _cleanupLoggingTimeoutMs=CleanupLoggingTimeoutMs;
            _runners=new Dictionary<int, RunnerInfo>();
            _runningDisposeTasks=new HashSet<Task>();
        }

        public void RegisterSession(IActiveSession SessionKey)
        {
            if (_sessionKey==null) _sessionKey=SessionKey;
            else CheckSession(SessionKey);
        }


        public void RegisterRunner(IActiveSession SessionKey, int RunnerNumber, IRunner Runner, Type ResultType, String TraceIdentifier)
        {
            CheckSession(SessionKey);
            CheckDisposed();
            RunnerId runner_id = new RunnerId(SessionKey, RunnerNumber);
            #if TRACE
            _logger?.LogTraceRegisterRunnerEnter(runner_id, TraceIdentifier);
            #endif
            lock(RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceRegisterRunnerLockAcquired(runner_id, TraceIdentifier);
                #endif
                if (_cleanup_mark) {
                    _logger?.LogWarningRegisterRunnerAfterCleanupInit(runner_id, TraceIdentifier);
                    throw new InvalidOperationException("Attempt to register a runner after cleanup initiation.");
                }
                _runnersCounter.AddCount();
                _runners.Add(RunnerNumber, new RunnerInfo(Runner, ResultType, RunnerNumber));
            }
            _logger?.LogDebugNewRunnerAvailable(runner_id, TraceIdentifier);
            #if TRACE
            _logger?.LogTraceRegisterRunnerExit(runner_id, TraceIdentifier);
            #endif
        }

        public Task? UnregisterRunner(IActiveSession SessionKey, int RunnerNumber)
        {
            CheckSession(SessionKey);
            CheckDisposed();
            RunnerId runner_id = new RunnerId(SessionKey, RunnerNumber);
            #if TRACE
            _logger?.LogTraceUnregisterRunner(runner_id);
            #endif
            Task? finish_cleanup = null;
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceUnregisterRunnerLockAcquired(runner_id);
                #endif
                RunnerInfo? info=null;
                if(_runners.TryGetValue(RunnerNumber, out info)) {
                    #if TRACE
                    _logger?.LogTraceUnregisterRunnerRemove(runner_id);
                    #endif
                    _runners.Remove(RunnerNumber);
                    _runnersCounter.Signal();
                    _logger?.LogDebugRunnerExecutionEnded(runner_id);
                    finish_cleanup=RunDisposeRunnerTask(info!, runner_id);
                }
                else
                    _logger?.LogDebugUnregisterRunnerNotRegistered(runner_id);
            }
            #if TRACE
            _logger?.LogTraceUnregisterRunnerExit(runner_id);
            #endif
            return finish_cleanup;
        }

        public void ReturnRunnerNumber(IActiveSession SessionKey, Int32 RunnerNumber)
        {
            CheckSession(SessionKey);
            #if TRACE
            _logger?.LogTraceReturnRunnerNumber(SessionKey.MakeSessionId(), RunnerNumber);
            #endif
            //Do nothing until RunnerNumber reusage is implemented
        }


        public Int32 GetNewRunnerNumber(IActiveSession SessionKey, String TraceIdentifier)
        {
            CheckSession(SessionKey);
            String session_id = SessionKey.MakeSessionId();
            #if TRACE
            _logger?.LogTraceGetNewRunnerNumber(session_id, TraceIdentifier);
            #endif
            if (_newRunnerNumber>=_maxRunnerNumber) {
                _logger?.LogErrorCannotAllocateRunnerNumber(session_id, TraceIdentifier);
                throw new InvalidOperationException("Cannot acquire a new runner number");
            }
            int result = _newRunnerNumber++;
            #if TRACE
            _logger?.LogTraceGetNewRunnerNumberExit(session_id, result, TraceIdentifier);
            #endif
            return result;
        }

        public Object RunnerCreationLock { get; init; } = new Object();

        public void Dispose()
        {
            if(Interlocked.Exchange(ref _disposed,1)!=0) return;
            _runnersCounter?.Dispose();
        }

        public RunnerInfo? GetRunnerInfo(IActiveSession SessionKey, Int32 RunnerNumber)
        {
            CheckSession(SessionKey);
            RunnerInfo? result;
            RunnerId runner_id = new RunnerId(SessionKey, RunnerNumber);
            #if TRACE
            _logger?.LogTraceGetRunnerInfo(runner_id);
            #endif
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceGetRunnerInfoLockAcquired(runner_id);
                #endif
                result=_runners.ContainsKey(RunnerNumber) ? _runners[RunnerNumber] : null;
            }
            #if TRACE
            _logger?.LogTraceGetRunnerInfoExit(runner_id, result!=null);
            #endif
            return result;
        }

        public void AbortAll(IActiveSession SessionKey)
        {
            CheckSession(SessionKey);
            String session_id = SessionKey.MakeSessionId();
            #if TRACE
            _logger?.LogTraceAbortAll(session_id);
            #endif
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceAbortAllLockAcqired(session_id);
                #endif
                foreach (var runner_info in _runners.Values) {
                    runner_info.Runner.Abort();
                    #if TRACE
                    _logger?.LogTraceAbortAllAbortRunner(new RunnerId(SessionKey, runner_info.Number));
                    #endif
                }
            }
            #if TRACE
            _logger?.LogTraceAbortAllExit(session_id);
            #endif
        }

        public async Task PerformRunnersCleanupAsync(IActiveSession SessionKey)
        {
            CheckDisposed();
            CheckSession(SessionKey);
            String session_id = SessionKey.MakeSessionId();
            Boolean has_runners;
            #if TRACE
            _logger?.LogTracePerformRunnersCleanup(session_id);
            #endif
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTracePerformRunnersCleanupAcquired(session_id);
                #endif
                if (_cleanup_mark) {
                    #if TRACE
                    _logger?.LogTracePerformRunnersCleanupDuplicate(session_id);
                    #endif
                    return;
                }
                _cleanup_mark=true;
                has_runners=_runners.Count>0; 
                if(has_runners) AbortAll(SessionKey);
                else {
                    //LogTrace no runners
                    #if TRACE
                    _logger?.LogTracePerformRunnersCleanupNoRunners(session_id);
                    #endif
                }
            }
            #if TRACE
            _logger?.LogTracePerformRunnersCleanupReleased(session_id);
            #endif
            _runnersCounter.Signal();
            if(has_runners) {
                Task unreg_task = new Task(WaitUnregistration, session_id);
                unreg_task.Start();
                #if TRACE
                _logger?.LogTracePerformRunnersCleanupAwaiting(session_id);
                #endif
                await unreg_task;
                Task[] dispose_tasks;
                #if TRACE
                _logger?.LogPerformRunnersCleanupAcquiringLock2(session_id);
                #endif
                lock (RunnerCreationLock) {
                    #if TRACE
                    _logger?.LogTracePerformRunnersCleanupLockAcquired2(session_id);
                    #endif
                    dispose_tasks=_runningDisposeTasks.ToArray();
                }
                #if TRACE
                _logger?.LogTracePerformRunnersCleanupLockReleased2(session_id);
                #endif
                await Task.WhenAll(dispose_tasks);
            }
            #if TRACE
            _logger?.LogTracePerformRunnersCleanupDisposing(session_id);
            #endif
            Dispose();
            #if TRACE
            _logger?.LogTracePerformRunnersCleanupComplete(session_id);
            #endif
        }

        public Task? GetRunnerCleanupTrackingTask(IActiveSession SessionKey, Int32 RunnerNumber)
        {
            Task? result=null;
            CheckDisposed();
            RunnerId runner_id = new RunnerId(SessionKey, RunnerNumber);
            #if TRACE
            _logger?.LogTraceGetRunnerCleanupTracking(runner_id);
            #endif
            lock(RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceGetRunnerCleanupTrackingLocked(runner_id);
                #endif
                RunnerInfo? info = GetRunnerInfo(SessionKey, RunnerNumber);
                if(info!=null) {
                    TaskCompletionSource tcs;
                    if(info.TrackCleanup==null) {
                        #if TRACE
                        _logger?.LogTraceGetRunnerCleanupTrackingNewTaskSource(runner_id);
                        #endif
                        info.TrackCleanup=new TaskCompletionSource();
                    }
                    tcs=info.TrackCleanup;
                    result=tcs.Task;
                }
            }
            #if TRACE
            _logger?.LogTraceGetRunnerCleanupTrackingExit(result!=null, runner_id);
            #endif
            return result;
        }

        void CheckSession(IActiveSession SessionKey)
        {
            if (!ReferenceEquals(_sessionKey, SessionKey))
                throw new InvalidOperationException("DefaultRunnerManager can serve runners from one session only/");
        }

        Task? RunDisposeRunnerTask(RunnerInfo RunnerInfo, RunnerId RunnerId) 
            //This method is always performed with RunnerCreationLock acquired
        {
            #if TRACE
            _logger?.LogTracePrepareDisposeTask(RunnerId);
            #endif
            Task? dispose_task=null;
            if (RunnerInfo.Runner is IAsyncDisposable async_disposable) {
                dispose_task=async_disposable.DisposeAsync().AsTask();
                #if TRACE
                _logger?.LogTraceRunAsyncDisposeTask(RunnerId);
                #endif
            }
            else if (RunnerInfo.Runner is IDisposable disposable) {
                dispose_task=Task.Run(() => disposable.Dispose());
                #if TRACE
                _logger?.LogTraceRunDisposeTask(RunnerId);
                #endif
            }
            if (dispose_task!=null) {
                _runningDisposeTasks.Add(dispose_task);
                Task? result= dispose_task.ContinueWith(FinishDisposalTaskBody, RunnerId);
                if(RunnerInfo.TrackCleanup!=null)
                    result.ContinueWith(_ => RunnerInfo.TrackCleanup.SetResult());
                Utilities.TimeoutLoggerContext tl_info = new Utilities.TimeoutLoggerContext(result,
                    #if TRACE
                    () =>_logger?.LogTraceRunnerCompleteDispose(RunnerId),
                    #else
                    ()=>{},
                    #endif
                    wait_ok=> _logger?.LogDebugRunnerEndWaitingForCleanup(RunnerId, wait_ok)
                );
                if(_cleanupLoggingTimeoutMs!=null)
                    _cleanupLoggingTask=Task.WhenAny(result, Task.Delay(_cleanupLoggingTimeoutMs.Value))
                        .ContinueWith(Utilities.TimeoutLoggerBody, tl_info);
                else _cleanupLoggingTask=Task.WhenAny(result).ContinueWith(Utilities.TimeoutLoggerBody, tl_info); ;

                return result;
            }
            else {
                #if TRACE
                _logger?.LogTraceRunNoDisposeTask(RunnerId);
                #endif
                _logger?.LogDebugRunnerEndWaitingForCleanup(RunnerId, true);
                if(RunnerInfo.TrackCleanup!=null) RunnerInfo.TrackCleanup.SetResult();
                return null;
            }
        }

        void FinishDisposalTaskBody(Task Antecedent, Object? State)
        {
            RunnerId runner_id = (RunnerId)State!;
            #if TRACE
            _logger?.LogTraceFinishDisposalTask(runner_id);
            #endif
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceFinishDisposalTaskLockAcquired(runner_id);
                #endif
                _runningDisposeTasks.Remove(Antecedent);
            }
            Antecedent.Dispose();
            #if TRACE
            _logger?.LogTraceFinishDisposalTaskExit(runner_id);
            #endif
        }

        void WaitUnregistration(Object? State)
        {
            String session_id = (String)State!;
            #if TRACE
            _logger?.LogTraceCleanupRunnersWaitForUnregistration(session_id);
            #endif
            _runnersCounter.Wait();
            #if TRACE
            _logger?.LogTraceCleanupRunnersExit(session_id);
            #endif
        }

        internal Boolean IsDisposed() => Volatile.Read(ref _disposed)!=0; //internal - for tests

        void CheckDisposed()
        {
            if (IsDisposed())
            if (IsDisposed())
                throw new ObjectDisposedException(this.GetType().FullName);
        }

    }
}
