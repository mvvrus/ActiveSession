using MVVrus.AspNetCore.ActiveSession.Internal;
using System;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// TODO                                                                                     
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public sealed class SessionProcessRunner<TResult> : RunnerBase, IRunner<TResult>, IRunnerBackgroundProgress
    {
        TResult _result=default!;
        Int32 _progress = 0;
        Int32? _estimatedEnd=null;
        Object _lock = new Object();
        PriorityQueue<TaskListItem, Int32> _waitList = new PriorityQueue<TaskListItem, Int32>();
        readonly Func<Action<TResult, Int32?>, CancellationToken, Task> _taskToRun;
        RunnerStatus _backgroundStatus = RunnerStatus.Stalled;
        Exception? _backgroundException = null;
        internal Task? _bkgCompletionTask;  //internal asccess modifier is for test project access

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="TaskBody"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Logger"></param>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            Func<Action<TResult, Int32?>, CancellationToken, TResult> TaskBody, RunnerId RunnerId, ILogger? Logger) :
            this(MakeTaskToRun(TaskBody), RunnerId, Logger) {}


        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="TaskBody"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Logger"></param>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            Action<Action<TResult, Int32?>, CancellationToken> TaskBody, RunnerId RunnerId, ILogger? Logger):
            this(MakeTaskToRun(TaskBody), RunnerId,Logger) {}

        SessionProcessRunner(
            Func<Action<TResult, Int32?>,CancellationToken,Task> TaskToRun, RunnerId RunnerId, ILogger? Logger) 
            : base(new CancellationTokenSource(), true, RunnerId, Logger)
        {
            _taskToRun = TaskToRun;
            StartRunning();
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected internal override void StartBackgroundExecution()
        {
            Task t = _taskToRun(SetProgress, CompletionToken);
            _bkgCompletionTask=t.ContinueWith(SessionTaskCompletionHandler,TaskContinuationOptions.ExecuteSynchronously);
            if(t.Status == TaskStatus.Created) try { t.Start(); } catch(TaskSchedulerException) { }
        }

        static Func<Action<TResult, Int32?>, CancellationToken, Task> MakeTaskToRun(
            Action<Action<TResult, Int32?>, CancellationToken> TaskBody)
        {
            return (ProgressSetter,Token)=>new Task(
                State => TaskBody(
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item1,
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item2),
                (ProgressSetter, Token),Token);
        }

        static Func<Action<TResult, Int32?>, CancellationToken, Task> MakeTaskToRun(
            Func<Action<TResult, Int32?>, CancellationToken,TResult> TaskBody)
        {
            return (ProgressSetter, Token) => new Task<TResult>(
                State => TaskBody(
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item1,
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item2)
                , (ProgressSetter, Token), Token)
            ;
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
            lock (_lock) {
                CheckAndNormalizeParams(ref Advance, ref StartPosition, nameof(GetAvailable));
                Int32 max_advance = _progress-StartPosition;
                RunnerStatus new_status;
                if(max_advance<=Advance) {
                    new_status=_backgroundStatus; //Stalled or one of final 
                    Position =_progress;
                }
                else {
                    new_status=RunnerStatus.Progressed;
                    Position =StartPosition+Advance;
                }
                if(SetStatus(new_status) && new_status==RunnerStatus.Failed) Exception=_backgroundException; 
                return new RunnerResult<TResult>(_result, Status, Position , Status == RunnerStatus.Failed ? Exception : null);
            }
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
            lock(_lock) {
                int max_advance;
                CheckAndNormalizeParams(ref Advance, ref StartPosition, nameof(GetRequiredAsync));
                max_advance = Math.Min(Advance, Int32.MaxValue-StartPosition);
                if(IsBackgroundExecutionCompleted || max_advance <= _progress-StartPosition) { //Synchronous path
                    Position  = Math.Min(_progress, StartPosition + max_advance);
                    RunnerStatus new_status = Position  < _progress ? RunnerStatus.Progressed : _backgroundStatus;
                    if(SetStatus(new_status) && new_status==RunnerStatus.Failed) Exception=_backgroundException;
                    return new ValueTask<RunnerResult<TResult>>(
                        new RunnerResult<TResult>(_result, Status, Position , Status == RunnerStatus.Failed ? Exception : null));
                }
                else { //Asynchronous path
                    TaskListItem item = new TaskListItem
                    {
                        TaskSourceToComplete = new TaskCompletionSource<RunnerResult<TResult>>(),
                        Token = Token,
                        DesiredPosition = StartPosition+max_advance,
                        TraceIdentifier = TraceIdentifier
                    };
                    if(Token.CanBeCanceled) item.Registration = Token.Register(CancelATask, item);
                    _waitList.Enqueue(item, item.DesiredPosition);
                    return new ValueTask<RunnerResult<TResult>>(item.TaskSourceToComplete.Task);

                }

            }
        }

        /// <inheritdoc/>
        public (Int32 Progress, Int32? EstimatedEnd) GetProgress()
        {
            lock(_lock) return (_progress, _estimatedEnd);
        }


        /// <inheritdoc/>
        public Boolean IsBackgroundExecutionCompleted { get; private set; } = false;

        void CheckAndNormalizeParams(ref Int32 Advance,ref Int32 StartPosition, String MethodName)
        {
            if(StartPosition == IRunner.CURRENT_POSITION) StartPosition = Position ;
            if(StartPosition < Position ) {
                String classname = Utilities.MakeClassCategoryName(GetType());
                throw new ArgumentException(nameof(StartPosition),
                        $"{classname}.{MethodName}:StartPosition value ({StartPosition}) is behind current runner Position({Position})");
            }
            if(Advance<0) {
                String classname = Utilities.MakeClassCategoryName(GetType());
                throw new ArgumentException(nameof(Advance),
                        $"{classname}.{MethodName}:Advance value ({Advance}) is negative");

            }
            if(StartPosition==Position  && Advance == IRunner.DEFAULT_ADVANCE) Advance = 1;
        }

        void CancelATask(object? Item)
        {
            TaskListItem item = Item as TaskListItem ?? throw new ArgumentNullException(nameof(Item)); //TODO think: sould we throw here?
            item.TaskSourceToComplete?.TrySetCanceled();
            item.TaskSourceToComplete = null; //To skip completing the task defined by this item.TaskSourceToComplete
                                              // in the AdvanceProgress method
        }

        internal void AdvanceProgress()
        {
            _progress++;
            SetStatus(RunnerStatus.Progressed);
            TaskListItem? task_item;
            Int32 task_position;
            //Complete tasks that wait for reaching this position
            while(_waitList.TryPeek(out task_item, out task_position) && (Status.IsFinal() || task_position <= _progress)) {
                task_item.Registration?.Dispose();
                _waitList.Dequeue();
                if(task_item.Token.IsCancellationRequested) task_item!.TaskSourceToComplete?.TrySetCanceled();
                else {
                    Position=task_position;
                    SetStatus(Position<_progress ? RunnerStatus.Progressed : RunnerStatus.Stalled);
                    task_item!.TaskSourceToComplete?.TrySetResult(
                        new RunnerResult<TResult>(_result, Status, Position, Status == RunnerStatus.Failed ? Exception : null));
                }
            }
        }

        void SessionTaskCompletionHandler(Task Antecedent)
        {
            TaskListItem? task_item;
            Int32 task_position;
            lock(_lock) {
                IsBackgroundExecutionCompleted = true;
                switch(Antecedent.Status) {
                    case TaskStatus.RanToCompletion:
                        //SetStatus(RunnerStatus.Complete);
                        _backgroundStatus=RunnerStatus.Complete;
                        _progress++;
                        if(Antecedent is Task<TResult> task_with_result) _result = task_with_result.Result;
                        break;
                    case TaskStatus.Faulted:
                        Exception? exception = Antecedent.Exception;
                        if(exception != null && exception is AggregateException aggregate_exception)
                            if(aggregate_exception.InnerExceptions.Count == 1) exception = aggregate_exception.InnerExceptions[0];
                        _backgroundStatus=RunnerStatus.Failed;
                        _backgroundException=exception;
                        break;
                    case TaskStatus.Canceled:
                        SetStatus(RunnerStatus.Aborted);
                        break;
                    default:
                        //TODO Something went wrong, LogError
                        while(_waitList.TryDequeue(out task_item, out task_position)) {
                            Exception e = new Exception("Internal error in SessionTaskCompletionHandler");
                            task_item!.TaskSourceToComplete?.TrySetException(e);
                        }
                        return;
                }
                while(_waitList.TryDequeue(out task_item, out task_position)) {
                    Position=_progress;
                    if(SetStatus(_backgroundStatus) && _backgroundStatus==RunnerStatus.Failed) Exception=_backgroundException;
                    task_item!.TaskSourceToComplete?
                        .TrySetResult(new RunnerResult<TResult>(
                            _result, Status, _progress, Status == RunnerStatus.Failed ? Exception : null));
                }
            }
        }

        void SetProgress(TResult Result, Int32? EstimatedEnd)
        {
            lock(_lock) {
                _result = Result;
                _estimatedEnd = EstimatedEnd;
                AdvanceProgress();
            }
        }

        record TaskListItem
        {
            public TaskCompletionSource<RunnerResult<TResult>>? TaskSourceToComplete;
            public CancellationToken Token;
            public CancellationTokenRegistration? Registration;
            public Int32 DesiredPosition;   
            public String? TraceIdentifier; 
        }
    }
}
