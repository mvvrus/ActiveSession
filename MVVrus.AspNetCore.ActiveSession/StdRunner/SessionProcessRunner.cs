using MVVrus.AspNetCore.ActiveSession.Internal;
using System;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// TODO                                                                                     
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public sealed class SessionProcessRunner<TResult> : RunnerBase, IRunner<TResult>, IAsyncDisposable
    {
        TResult _result=default!;
        Int32 _progress = 0;
        Int32? _estimatedEnd=null;
        Object _lock = new Object();
        PriorityQueue<TaskListItem, Int32> _waitList = new PriorityQueue<TaskListItem, Int32>();
        readonly Func<Action<TResult, Int32?>, CancellationToken, Task> _taskToRunCreator;
        RunnerStatus _backgroundStatus = RunnerStatus.Stalled;
        Exception? _backgroundException = null;
        internal Task? _bkgCompletionTask;  //internal asccess modifier is for test project access
        Boolean _isBackgroundExecutionCompleted=false;
        Boolean _bkgTaskReturnsResult = false;
        Boolean _bkgSynchronous = false;
        Task? _disposeTask;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="ProcessTaskBody"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(Func<Action<TResult, Int32?>, CancellationToken, TResult> ProcessTaskBody, 
            RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this((ProcessTaskBody??throw new ArgumentNullException(nameof(ProcessTaskBody)),
                null, true), RunnerId, LoggerFactory) {}

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Param"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            (Func<Action<TResult, Int32?>, CancellationToken, TResult> ProcessTaskBody, CancellationTokenSource? Cts, Boolean PassCtsOwnership) Param, 
            RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this(MakeTaskToRunCreator(Param.ProcessTaskBody??throw new ArgumentNullException(nameof(Param.ProcessTaskBody))),
                Param.Cts, Param.PassCtsOwnership, RunnerId,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(SessionProcessRunner<TResult>)))) 
        {
            _bkgTaskReturnsResult=true;
            _bkgSynchronous=true;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="ProcessTaskBody"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            Action<Action<TResult, Int32?>, CancellationToken> ProcessTaskBody, RunnerId RunnerId, ILoggerFactory? LoggerFactory):
            this((ProcessTaskBody??throw new ArgumentNullException(nameof(ProcessTaskBody)), null, true),
                RunnerId, LoggerFactory) {}

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Param"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            (Action<Action<TResult, Int32?>, CancellationToken> ProcessTaskBody, 
            CancellationTokenSource? Cts, Boolean PassCtsOwnership) Param, RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this(MakeTaskToRunCreator(Param.ProcessTaskBody??throw new ArgumentNullException(nameof(Param.ProcessTaskBody))),
                Param.Cts, Param.PassCtsOwnership, RunnerId,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(SessionProcessRunner<TResult>))))
        {
            _bkgSynchronous=true;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="ProcessTaskCreator"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>> ProcessTaskCreator, 
            RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this((ProcessTaskCreator??throw new ArgumentNullException(nameof(ProcessTaskCreator)), null, true), RunnerId, LoggerFactory)
        {}

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="ProcessTaskCreator"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(Func<Action<TResult, Int32?>, CancellationToken, Task> ProcessTaskCreator, 
            RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this((ProcessTaskCreator??throw new ArgumentNullException(nameof(ProcessTaskCreator)), null, true), RunnerId, LoggerFactory)
        { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Param"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            (Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>> ProcessTaskCreator, CancellationTokenSource? Cts, Boolean PassCtsOwnership) Param, 
            RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this(Param.ProcessTaskCreator??throw new ArgumentNullException(nameof(Param.ProcessTaskCreator)), null, true, RunnerId,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(SessionProcessRunner<TResult>))))
        {
            _bkgTaskReturnsResult=true;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Param"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            (Func<Action<TResult, Int32?>, CancellationToken, Task> ProcessTaskCreator, CancellationTokenSource? Cts, Boolean PassCtsOwnership) Param,
            RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this(Param.ProcessTaskCreator??throw new ArgumentNullException(nameof(Param.ProcessTaskCreator)), null, true, RunnerId,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(SessionProcessRunner<TResult>))))
        { }

        SessionProcessRunner(
            Func<Action<TResult, Int32?>,CancellationToken,Task> TaskToRunCretator, 
            CancellationTokenSource? CompletionTokenSource, Boolean PassCtsOwnership,
            RunnerId RunnerId, ILogger? Logger) 
            : base(CompletionTokenSource, PassCtsOwnership, RunnerId, Logger)
        {
            Logger?.LogDebugSessionRunnerConstructor(RunnerId, CompletionTokenSource!=null, PassCtsOwnership, _bkgSynchronous, _bkgTaskReturnsResult);
            _taskToRunCreator = TaskToRunCretator;
            StartRunning();
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected internal override void StartBackgroundExecution()
        {
            #if TRACE
            Logger?.LogTraceSessionProcessStartBackgroundExecution(Id);
            #endif
            Task t = _taskToRunCreator(SetProgress, CompletionToken);
            _bkgCompletionTask=t.ContinueWith(SessionTaskCompletionHandler,TaskContinuationOptions.ExecuteSynchronously);
            if(t.Status == TaskStatus.Created) { 
                try { t.Start(); } catch(TaskSchedulerException) { }
                #if TRACE
                Logger?.LogTraceSessionProcessStartBackgroundTask(Id);
                #endif
            }
            #if TRACE
            Logger?.LogTraceSessionProcessStartBackgroundExecutionExit(Id);
            #endif
        }

        static Func<Action<TResult, Int32?>, CancellationToken, Task> MakeTaskToRunCreator(
            Action<Action<TResult, Int32?>, CancellationToken> TaskBody)
        {
            return (ProgressSetter,Token)=>new Task(
                State => TaskBody(
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item1,
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item2),
                (ProgressSetter, Token),Token);
        }

        static Func<Action<TResult, Int32?>, CancellationToken, Task> MakeTaskToRunCreator(
            Func<Action<TResult, Int32?>, CancellationToken,TResult> TaskBody)
        {
            return (ProgressSetter, Token) => new Task<TResult>(
                State => TaskBody(
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item1,
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item2)
                , (ProgressSetter, Token), Token)
            ;
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected override void PreDispose()
        {
            #if TRACE
            Logger?.LogTraceSessionProcessAbortBkgTask(Id);
            #endif
            Abort();
            base.PreDispose();
        }

        async Task DisposeAsyncCore()
        {
            #if TRACE
            Logger?.LogTraceSessionProcessDisposing(Id);
            #endif
            if(_bkgCompletionTask!=null) {
                await _bkgCompletionTask!;
                #if TRACE
                Logger?.LogTraceSessionProcessBkgTaskAwaited(Id);
                #endif
            }
            base.Dispose(true);
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            if(SetDisposed()) {
                #if TRACE
                Logger?.LogTraceSessionProcessDisposeAsync(Id);
                #endif
                _disposeTask = DisposeAsyncCore();
            }
            return _disposeTask!.IsCompleted ? ValueTask.CompletedTask : new ValueTask(_disposeTask!);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Disposing"></param>
        protected sealed override void Dispose(Boolean Disposing)
        {
            DisposeAsyncCore().Wait();
        }

        // <inheritdoc/>: Invalid cref value "!:TResult" found in triple-slash-comments for GetAvailable 
        /// <summary>
        ///  TODO
        /// </summary>
        /// <param name="Advance"></param>
        /// <param name="StartPosition"></param>
        /// <param name="TraceIdentifier"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public RunnerResult<TResult> GetAvailable(Int32 Advance = IRunner.MAXIMUM_ADVANCE, Int32 StartPosition = IRunner.CURRENT_POSITION, String? TraceIdentifier = null)
        {
            RunnerResult<TResult> result;
            String trace_identifier = TraceIdentifier??ActiveSessionConstants.UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            Logger?.LogTraceSessionProcessGetAvailableEntered(Id, trace_identifier);
            #endif
            lock(_lock) {
                CheckDisposed();
                #if TRACE
                Logger?.LogTraceSessionProcessGetAvailableLockAckuired(Id, trace_identifier);
                #endif
                CheckAndNormalizeParams(ref Advance, ref StartPosition, nameof(GetAvailable), trace_identifier);
                if(!Status.IsFinal()) {
                    Int32 max_advance = _progress-StartPosition;
                    RunnerStatus new_status;
                    if(max_advance<=Advance) {
                        new_status=_backgroundStatus; //Stalled or a final 
                        Position =_progress;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetAvailableAll(Id,trace_identifier);
                        #endif
                    }
                    else {
                        new_status=RunnerStatus.Progressed;
                        Position =StartPosition+Advance;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetAvailableNotAll(Id,trace_identifier);
                        #endif
                    }
                    #if TRACE
                    Logger?.LogTraceSessionProcessGetAvailableTrySetNewStatus(Id,trace_identifier);
                    #endif
                    if(SetStatus(new_status)) {
                        if(new_status==RunnerStatus.Failed)  Exception=_backgroundException;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetAvailableNewStatusSet(Id,trace_identifier);
                        #endif
                    }
                }
                result = new RunnerResult<TResult>(_result, Status, Position , Status == RunnerStatus.Failed ? Exception : null);
            }
            #if TRACE
            Logger?.LogTraceSessionProcessGetAvailableLockReleased(Id,trace_identifier);
            #endif
            Logger?.LogDebugSessionProcessRunnerResult(result.FailureException, result.Result?.ToString()??"<null>", 
                result.Status, result.Position, Id, trace_identifier);
            return result;
        }

        // <inheritdoc/>: Invalid cref value "!:TResult" found in triple-slash-comments for GetRequiredAsync
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Advance"></param>
        /// <param name="Token"></param>
        /// <param name="StartPosition"></param>
        /// <param name="TraceIdentifier"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ValueTask<RunnerResult<TResult>> GetRequiredAsync(
            Int32 Advance = IRunner.DEFAULT_ADVANCE,
            CancellationToken Token = default,
            Int32 StartPosition = IRunner.CURRENT_POSITION,
            String? TraceIdentifier = null)
        {
            ValueTask<RunnerResult<TResult>> result_task;
            String trace_identifier = TraceIdentifier??ActiveSessionConstants.UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            Logger?.LogTraceSessionProcessGetRequiredAsyncEntered(Id, trace_identifier);
            #endif
            lock(_lock) {
                CheckDisposed();
                #if TRACE
                Logger?.LogTraceSessionProcessGetRequiredAsyncLockAckuired(Id, trace_identifier);
                #endif
                int max_advance;
                CheckAndNormalizeParams(ref Advance, ref StartPosition, nameof(GetRequiredAsync), trace_identifier);
                max_advance = Math.Min(Advance, Int32.MaxValue-StartPosition);
                if(Status.IsFinal() || IsBackgroundExecutionCompleted || max_advance <= _progress-StartPosition) { //Synchronous
                    #if TRACE
                    Logger?.LogTraceSessionProcessGetRequiredAsyncSynchronous(Id, trace_identifier);
                    #endif
                    Position  = Math.Min(_progress, StartPosition + max_advance);
                    RunnerStatus new_status; if(Position  < _progress) {
                        new_status = RunnerStatus.Progressed;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetRequiredAsyncNotAll(Id, trace_identifier);
                        #endif
                    }
                    else {
                        new_status=_backgroundStatus;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetRequiredAsyncAll(Id,trace_identifier);
                        #endif
                    }
                    #if TRACE
                    Logger?.LogTraceSessionProcessGetRequiredAsyncTrySetNewStatus(Id, trace_identifier);
                    #endif
                    if(SetStatus(new_status)) {
                        if(new_status==RunnerStatus.Failed) Exception=_backgroundException;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetRequiredAsyncNewStatusSet(Id, trace_identifier);
                        #endif
                    }
                    RunnerResult<TResult> result = new RunnerResult<TResult>(_result, Status, Position, Status == RunnerStatus.Failed ? Exception : null);
                    Logger?.LogDebugSessionProcessRunnerResult(result.FailureException, result.Result?.ToString()??"<null>",
                        result.Status, result.Position, Id, trace_identifier);
                    result_task=new ValueTask<RunnerResult<TResult>>(result);
                }
                else { //Asynchronous path
                    #if TRACE
                    Logger?.LogTraceSessionProcessGetRequiredAsyncAsynchronous(Id, trace_identifier);
                    #endif
                    TaskListItem task_item = new TaskListItem
                    {
                        TaskSourceToComplete = new TaskCompletionSource<RunnerResult<TResult>>(),
                        Token = Token,
                        DesiredPosition = StartPosition+max_advance,
                        TraceIdentifier = trace_identifier
                    };
                    if(Token.CanBeCanceled) task_item.Registration = Token.Register(CancelATask, task_item);
                    _waitList.Enqueue(task_item, task_item.DesiredPosition);
                    #if TRACE
                    Logger?.LogTraceSessionProcessGetRequiredAsyncTaskEnqueued(Id, trace_identifier);
                    #endif
                    result_task=new ValueTask<RunnerResult<TResult>>(task_item.TaskSourceToComplete.Task);
                }
            }
            #if TRACE
            Logger?.LogTraceSessionProcessGetRequiredAsyncLockReleased(Id, trace_identifier);
            #endif
            return result_task;
        }

        /// <inheritdoc/>
        public override RunnerBkgProgress GetProgress()
        {
            lock(_lock) return (_progress, _estimatedEnd);
        }


        /// <inheritdoc/>
        public override Boolean IsBackgroundExecutionCompleted { get => _isBackgroundExecutionCompleted; }

        void CheckAndNormalizeParams(ref Int32 Advance,ref Int32 StartPosition, String MethodName, String TraceIdentifier)
        {
            Exception? ex = null;
            #if TRACE
            Logger?.LogTraceSessionProcessCheckAndNormalizeParams(Id,TraceIdentifier);
            #endif
            if(StartPosition == IRunner.CURRENT_POSITION) StartPosition = Position ;
            if(StartPosition < Position ) {
                String classname = Utilities.MakeClassCategoryName(GetType());
                ex=new ArgumentException(nameof(StartPosition),
                        $"{classname}.{MethodName}:StartPosition value ({StartPosition}) is behind current runner Position({Position})");
                Logger?.LogWarningSessionProcessBadParameters(ex,Id,TraceIdentifier);
                throw ex;
            }
            if(Advance<0) {
                String classname = Utilities.MakeClassCategoryName(GetType());
                ex=new ArgumentException(nameof(Advance),
                        $"{classname}.{MethodName}:Advance value ({Advance}) is negative");
                Logger?.LogWarningSessionProcessBadParameters(ex,Id,TraceIdentifier);
                throw ex;
            }
            if(StartPosition==Position  && Advance == IRunner.DEFAULT_ADVANCE) {
                Advance = 1;
                #if TRACE
                Logger?.LogTraceSessionProcessCheckAndNormalizeParamsDefaultAdjusted(Id,TraceIdentifier);
                #endif
            }
#if TRACE
            Logger?.LogTraceSessionProcessCheckAndNormalizeParamsExit(Id,TraceIdentifier);
            #endif
        }

        void CancelATask(object? Item)
        {
            TaskListItem task_item = Item as TaskListItem ?? throw new ArgumentNullException(nameof(Item)); 
            if(task_item.TaskSourceToComplete?.TrySetCanceled()??false) {
                #if TRACE
                Logger?.LogTraceSessionProcessPendingTaskSetCanceled(Id,task_item.TraceIdentifier);
                #endif
            }
            else {
                Logger?.LogInfoTaskOutcomeAlreadySet(Id,task_item.TraceIdentifier);
            }
            task_item.TaskSourceToComplete = null; //To skip completing the task defined by this task_item.TaskSourceToComplete
                                              // in the AdvanceProgress method
        }

        void SessionTaskCompletionHandler(Task Antecedent)
        {
            TaskListItem? task_item;
            Int32 task_position;
            #if TRACE
            Logger?.LogTraceSessionProcessBkgEnded(Id);
            #endif
            lock(_lock) {
                #if TRACE
                Logger?.LogTraceSessionProcessBkgEndedLockAcquired(Id);
                #endif
                _isBackgroundExecutionCompleted = true;
                switch(Antecedent.Status) {
                    case TaskStatus.RanToCompletion:
                        #if TRACE
                        Logger?.LogTraceSessionProcessBkgEndedRanToCompletion(Id);
                        #endif
                        _backgroundStatus=RunnerStatus.Complete;
                        _progress++;
                        if(_bkgTaskReturnsResult) {
                            #if TRACE
                            Logger?.LogTraceSessionProcessBkgEndedAcceptResult(Id);
                            #endif
                            _result = ((Task<TResult>)Antecedent).Result;
                        }
                        break;
                    case TaskStatus.Faulted:
                        Exception? exception = Antecedent.Exception;
                        if(exception != null && exception is AggregateException aggregate_exception)
                            if(aggregate_exception.InnerExceptions.Count == 1) exception = aggregate_exception.InnerExceptions[0];
                        if(exception is OperationCanceledException) {
                            #if TRACE
                            Logger?.LogTraceSessionProcessBkgEndedCanceled(Id);
                            #endif
                            SetStatus(RunnerStatus.Aborted);
                        }
                        else {
                            #if TRACE
                            Logger?.LogTraceSessionProcessBkgEndedFaulted(exception, Id);
                            #endif
                            _backgroundStatus=RunnerStatus.Failed;
                            _backgroundException=exception;
                        }
                        break;
                    case TaskStatus.Canceled:
                        #if TRACE
                        Logger?.LogTraceSessionProcessBkgEndedCanceled(Id);
                        #endif
                        SetStatus(RunnerStatus.Aborted);
                        break;
                    default:
                        Logger?.LogErrorSessionProgressBkgEndedInternal(Antecedent.Status, Id);
                        String msg = $"Internal error in SessionTaskCompletionHandler: attempt to continue a task with Status={Antecedent.Status}";
                        while(_waitList.TryDequeue(out task_item, out task_position)) {
                            if(task_item.TaskSourceToComplete!=null) {
                                Exception e = new InvalidOperationException(msg);
                                if(task_item!.TaskSourceToComplete.TrySetException(e)) {
                                    #if TRACE
                                    Logger?.LogTraceSessionProcessPendingTaskSetException(e,Id,task_item.TraceIdentifier);
                                    #endif
                                };
                            }
                        }
                        return;
                }
                _estimatedEnd = _progress;
                #if TRACE
                Logger?.LogTraceSessionProcessBkgEndedCompletePendingTasks(Id);
                #endif
                while(_waitList.TryDequeue(out task_item, out task_position)) {
                    if(task_item.TaskSourceToComplete!=null) {
                        #if TRACE
                        Logger?.LogTraceSessionProcessBkgEndedCompleteAPendingTask(Id,task_item.TraceIdentifier);
                        #endif
                        if(Disposed()) {
                            Exception e = new ObjectDisposedException(DisposedObjectName());
                            if(task_item!.TaskSourceToComplete.TrySetException(e)) {
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetException(e,Id,task_item.TraceIdentifier);
                                #endif
                            }
                            else {
                                Logger?.LogInfoTaskOutcomeAlreadySet(Id,task_item.TraceIdentifier);
                            }
                        }
                        else {
                            RunnerStatus status=_backgroundStatus;
                            if(Status.IsFinal()) status=Status;
                            RunnerResult<TResult> result = new RunnerResult<TResult>(
                                _result, status, _progress, status == RunnerStatus.Failed ? _backgroundException : null);
                            if(task_item!.TaskSourceToComplete.TrySetResult(result)) {
                                Position=_progress;
                                if(SetStatus(_backgroundStatus) && _backgroundStatus==RunnerStatus.Failed) Exception=_backgroundException;
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetResult(Id,task_item.TraceIdentifier);
                                #endif
                                Logger?.LogDebugSessionProcessRunnerResult(result.FailureException, result.Result?.ToString()??"<null>",
                                    result.Status, result.Position, Id, task_item.TraceIdentifier);
                            }
                            else {
                                Logger?.LogInfoTaskOutcomeAlreadySet(Id,task_item.TraceIdentifier);
                            }
                        }
                    }
                    else {
                        #if TRACE
                        Logger?.LogTraceSessionProcessPendingTaskAlreadyCanceled(Id,task_item.TraceIdentifier);
                        #endif
                    }
                }
            }
            #if TRACE
            Logger?.LogTraceSessionProcessBkgEndedExit(Id);
            #endif

        }

        void SetProgress(TResult Result, Int32? EstimatedEnd)
        {
            #if TRACE
            Logger?.LogTraceSessionProcessCallback(Id);
            #endif
            if(CompletionToken.IsCancellationRequested) {
                #if TRACE
                Logger?.LogTraceSessionProcessCallbackCanceled(Id);
                #endif
                CompletionToken.ThrowIfCancellationRequested();
            }
            lock(_lock) {
                #if TRACE
                Logger?.LogTraceSessionProcessCallbackLockAcquired(Id);
                #endif
                _result = Result;
                _estimatedEnd = EstimatedEnd;
                _progress++;
                SetStatus(RunnerStatus.Progressed);
                TaskListItem? task_item;
                Int32 task_position;
                //Complete tasks that wait for reaching this position by background 
                #if TRACE
                Logger?.LogTraceSessionProcessCallbackCompletePendingTasks(Id);
                #endif
                while(_waitList.TryPeek(out task_item, out task_position) && (Status.IsFinal() || task_position <= _progress)) {
                    #if TRACE
                    Logger?.LogTraceSessionProcessCallbackCompleteAPendingTask(Id,task_item.TraceIdentifier);
                    #endif
                    task_item.Registration?.Dispose();
                    _waitList.Dequeue();
                    if(task_item.TaskSourceToComplete!=null) {
                        if(task_item.Token.IsCancellationRequested) {
                            if(task_item.TaskSourceToComplete.TrySetCanceled()) {
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetCanceled(Id,task_item.TraceIdentifier);
                                #endif
                            }
                            else {
                                Logger?.LogInfoTaskOutcomeAlreadySet(Id,task_item.TraceIdentifier);
                            }
                        }
                        else {
                            RunnerStatus old_status = Status;
                            SetStatus(task_position<_progress ? RunnerStatus.Progressed : RunnerStatus.Stalled);
                            RunnerResult<TResult> result = 
                                new RunnerResult<TResult>(_result, Status, task_position, Status == RunnerStatus.Failed ? Exception : null);
                            if(task_item!.TaskSourceToComplete.TrySetResult(result)) { 
                                Position=task_position;
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetResult(Id,task_item.TraceIdentifier);
                                #endif
                                Logger?.LogDebugSessionProcessRunnerResult(result.FailureException, result.Result?.ToString()??"<null>",
                                    result.Status, result.Position, Id, task_item.TraceIdentifier);
                            }
                            else {
                                SetStatus(old_status);
                                Logger?.LogInfoTaskOutcomeAlreadySet(Id,task_item.TraceIdentifier);
                            }
                        }
                    }
                    else {
                        #if TRACE
                        Logger?.LogTraceSessionProcessPendingTaskAlreadyCanceled(Id,task_item.TraceIdentifier);
                        #endif
                    }
                }
            }
            #if TRACE
            Logger?.LogTraceSessionProcessCallbackExit(Id);
            #endif
        }

        record TaskListItem
        {
            public TaskListItem() 
            {
                TraceIdentifier=ActiveSessionConstants.UNKNOWN_TRACE_IDENTIFIER;
            }
            public TaskCompletionSource<RunnerResult<TResult>>? TaskSourceToComplete;
            public CancellationToken Token;
            public CancellationTokenRegistration? Registration;
            public Int32 DesiredPosition;   
            public String TraceIdentifier; //Really not null
        }
    }
}
