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
        Boolean _cleanup_mark;
        Int32 _disposed;

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
                //TODO (future) Implement runner number reusage
            }
            _runners=new Dictionary<int, RunnerInfo>();
            _runningDisposeTasks=new HashSet<Task>();
        }

        public void RegisterSession(IActiveSession SessionKey)
        {
            if (_sessionKey==null) _sessionKey=SessionKey;
            else CheckSession(SessionKey);
        }


        public void RegisterRunner(IActiveSession SessionKey, int RunnerNumber, IRunner Runner, Type ResultType)
        {
            CheckSession(SessionKey);
            CheckDisposed();
            #if TRACE
            _logger?.LogTraceRegisterRunnerEnter(SessionKey.Id, RunnerNumber);
            #endif
            lock(RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceRegisterRunnerLockAcquired(SessionKey.Id, RunnerNumber);
                #endif
                if (_cleanup_mark) {
                    _logger?.LogWarningRegisterRunnerAfterCleanupInit(SessionKey.Id, RunnerNumber);
                    throw new InvalidOperationException("Attempt to register a runner after cleanup initiation.");
                }
                _runnersCounter.AddCount();
                _runners.Add(RunnerNumber, new RunnerInfo(Runner, ResultType, RunnerNumber));
            }
            #if TRACE
            _logger?.LogTraceRegisterRunnerExit(SessionKey.Id, RunnerNumber);
            #endif
        }

        public Task? UnregisterRunner(IActiveSession SessionKey, int RunnerNumber)
        {
            CheckSession(SessionKey);
            CheckDisposed();
            #if TRACE
            _logger?.LogTraceUnregisterRunner(SessionKey.Id, RunnerNumber);
            #endif
            Task? finish_cleanup = null;
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceUnregisterRunnerLockAcquired(SessionKey.Id, RunnerNumber);
                #endif
                RunnerInfo? info=null;
                if(_runners.TryGetValue(RunnerNumber, out info)) {
                    #if TRACE
                    _logger?.LogTraceUnregisterRunnerRemove(SessionKey.Id, RunnerNumber);
                    #endif
                    _runners.Remove(RunnerNumber);
                    _runnersCounter.Signal();
                    finish_cleanup=RunDisposeRunnerTask(info!, SessionKey.Id, RunnerNumber);
                }
                else
                    _logger?.LogDebugUnregisterRunnerNotRegistered(SessionKey.Id, RunnerNumber);
            }
            #if TRACE
            _logger?.LogTraceUnregisterRunnerExit(SessionKey.Id, RunnerNumber);
            #endif
            return finish_cleanup;
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

        public void Dispose()
        {
            if(Interlocked.Exchange(ref _disposed,1)!=0) return;
            _runnersCounter?.Dispose();
        }

        public RunnerInfo? GetRunnerInfo(IActiveSession SessionKey, Int32 RunnerNumber)
        {
            CheckSession(SessionKey);
            RunnerInfo? result;
            #if TRACE
            _logger?.LogTraceGetRunnerInfo(SessionKey.Id, RunnerNumber);
            #endif
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceGetRunnerInfoLockAcquired(SessionKey.Id, RunnerNumber);
                #endif
                result=_runners.ContainsKey(RunnerNumber) ? _runners[RunnerNumber] : null;
            }
            #if TRACE
            _logger?.LogTraceGetRunnerInfoExit(SessionKey.Id, RunnerNumber, result!=null);
            #endif
            return result;
        }

        public void AbortAll(IActiveSession SessionKey)
        {
            CheckSession(SessionKey);
            #if TRACE
            _logger?.LogTraceAbortAll(SessionKey.Id);
            #endif
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceAbortAllLockAcqired(SessionKey.Id);
                #endif
                foreach (var runner_info in _runners.Values) {
                    runner_info.Runner.Abort();
                    #if TRACE
                    _logger?.LogTraceAbortAllAbortRunner(SessionKey.Id, runner_info.Number);
                    #endif
                }
            }
            #if TRACE
            _logger?.LogTraceAbortAllExit(SessionKey.Id);
            #endif
        }

        public async Task PerformRunnersCleanupAsync(IActiveSession SessionKey)
        {
            CheckDisposed();
            CheckSession(SessionKey);
            Boolean has_runners;
            #if TRACE
            _logger?.LogTracePerformRunnersCleanup(SessionKey.Id);
            #endif
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTracePerformRunnersCleanupAcquired(SessionKey.Id);
                #endif
                if (_cleanup_mark) {
                    #if TRACE
                    _logger?.LogTracePerformRunnersCleanupDuplicate(SessionKey.Id);
                    #endif
                    return;
                }
                _cleanup_mark=true;
                has_runners=_runners.Count>0; 
                if(has_runners) AbortAll(SessionKey);
                else {
                    //LogTrace no runners
                    #if TRACE
                    _logger?.LogTracePerformRunnersCleanupNoRunners(SessionKey.Id);
                    #endif
                }
            }
            #if TRACE
            _logger?.LogTracePerformRunnersCleanupReleased(SessionKey.Id);
            #endif
            _runnersCounter.Signal();
            if(has_runners) {
                Task unreg_task = new Task(WaitUnregistration, SessionKey.Id);
                unreg_task.Start();
                #if TRACE
                _logger?.LogTracePerformRunnersCleanupAwaiting(SessionKey.Id);
                #endif
                await unreg_task;
                Task[] dispose_tasks;
                #if TRACE
                _logger?.LogPerformRunnersCleanupAcquiringLock2(SessionKey.Id);
                #endif
                lock (RunnerCreationLock) {
                    #if TRACE
                    _logger?.LogTracePerformRunnersCleanupLockAcquired2(SessionKey.Id);
                    #endif
                    dispose_tasks=_runningDisposeTasks.ToArray();
                }
                #if TRACE
                _logger?.LogTracePerformRunnersCleanupLockReleased2(SessionKey.Id);
                #endif
                await Task.WhenAll(dispose_tasks);
            }
            #if TRACE
            _logger?.LogTracePerformRunnersCleanupDisposing(SessionKey.Id);
            #endif
            Dispose();
            #if TRACE
            _logger?.LogTracePerformRunnersCleanupComplete(SessionKey.Id);
            #endif
        }

        public Task? GetRunnerCleanupTrackingTask(IActiveSession SessionKey, Int32 RunnerNumber)
        {
            Task? result=null;
            CheckDisposed();
            #if TRACE
            _logger?.LogTraceGetRunnerCleanupTracking(SessionKey.Id, RunnerNumber);
            #endif
            lock(RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceGetRunnerCleanupTrackingLocked(SessionKey.Id, RunnerNumber);
                #endif
                RunnerInfo? info = GetRunnerInfo(SessionKey, RunnerNumber);
                if(info!=null) {
                    TaskCompletionSource tcs;
                    if(info.TrackCleanup==null) {
                        #if TRACE
                        _logger?.LogTraceGetRunnerCleanupTrackingNewTaskSource(SessionKey.Id, RunnerNumber);
                        #endif
                        info.TrackCleanup=new TaskCompletionSource();
                    }
                    tcs=info.TrackCleanup;
                    result=tcs.Task;
                }
            }
            #if TRACE
            _logger?.LogTraceGetRunnerCleanupTrackingExit(result!=null, SessionKey.Id, RunnerNumber);
            #endif
            return result;
        }

        void CheckSession(IActiveSession SessionKey)
        {
            if (!ReferenceEquals(_sessionKey, SessionKey))
                throw new InvalidOperationException("DefaultRunnerManager can serve runners from one session only/");
        }

        Task? RunDisposeRunnerTask(RunnerInfo RunnerInfo, String SessionId, Int32 RunnerNumber) 
            //This method is always performed with RunnerCreationLock acquired
        {
            #if TRACE
            _logger?.LogTracePrepareDisposeTask(SessionId, RunnerNumber);
            #endif
            Task? dispose_task=null;
            if (RunnerInfo.Runner is IAsyncDisposable async_disposable) {
                dispose_task=async_disposable.DisposeAsync().AsTask();
                #if TRACE
                _logger?.LogTraceRunAsyncDisposeTask(SessionId, RunnerNumber);
                #endif
            }
            else if (RunnerInfo.Runner is IDisposable disposable) {
                dispose_task=Task.Run(() => disposable.Dispose());
                #if TRACE
                _logger?.LogTraceRunDisposeTask(SessionId, RunnerNumber);
                #endif
            }
            if (dispose_task!=null) {
                _runningDisposeTasks.Add(dispose_task);
                Task? result= dispose_task.ContinueWith(FinishDisposalTaskBody,new FinishInfo(SessionId,RunnerNumber));
                if(RunnerInfo.TrackCleanup!=null)
                    result.ContinueWith(_ => RunnerInfo.TrackCleanup.SetResult());
                return result;
            }
            else {
                #if TRACE
                _logger?.LogTraceRunNoDisposeTask(SessionId, RunnerNumber);
                #endif
                if(RunnerInfo.TrackCleanup!=null)
                    RunnerInfo.TrackCleanup.SetResult();
                return null;
            }
        }

        record FinishInfo(String SessionId, Int32 RunnerNumber);

        void FinishDisposalTaskBody(Task Antecedent, Object? State)
        {
            FinishInfo fi = (FinishInfo)State!;
            #if TRACE
            _logger?.LogTraceFinishDisposalTask(fi.SessionId, fi.RunnerNumber);
            #endif
            lock (RunnerCreationLock) {
                #if TRACE
                _logger?.LogTraceFinishDisposalTaskLockAcquired(fi.SessionId, fi.RunnerNumber);
                #endif
                _runningDisposeTasks.Remove(Antecedent);
            }
            Antecedent.Dispose();
            #if TRACE
            _logger?.LogTraceFinishDisposalTaskExit(fi.SessionId, fi.RunnerNumber);
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
