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
        Int32 _position = 0;
        PriorityQueue<TaskListItem, Int32> _waitList = new PriorityQueue<TaskListItem, Int32>();
        readonly Func<Action<TResult, Int32?>, CancellationToken, Task> _taskToRun;

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
            t.ContinueWith(SessionTaskCompletionHandler);
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
                    new_status=RunnerStatus.Stalled;
                    _position=_progress;
                }
                else {
                    new_status=RunnerStatus.Progressed;
                    _position=StartPosition+Advance;
                }
                SetStatus(new_status); //Just an attempt. If current status is a final one alredy, it'll never be changed
                return new RunnerResult<TResult>(_result, Status, _position, Status == RunnerStatus.Failed ? Exception : null);
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
                int new_position;
                CheckAndNormalizeParams(ref Advance, ref StartPosition, nameof(GetRequiredAsync));
                new_position = Position + Advance;
                if(Status.IsFinal() || new_position <= _progress) {
                    if(!Status.IsFinal()) _position = Math.Max(new_position, _position);
                    return new ValueTask<RunnerResult<TResult>>(
                        new RunnerResult<TResult>(_result, Status, _position, Status == RunnerStatus.Failed ? Exception : null));
                }
                else {
                    TaskListItem item = new TaskListItem
                    {
                        TaskSourceToComplete = new TaskCompletionSource<RunnerResult<TResult>>(),
                        Token = Token,
                        DesiredPosition = new_position,
                        TraceIdentifier = TraceIdentifier
                    };
                    if(Token.CanBeCanceled) item.Registration = Token.Register(CancelATask, item);
                    _waitList.Enqueue(item, new_position);
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

        /// <inheritdoc/>
        public override Int32 Position { get => _position;} //TODO Volatile.Read?

        void CheckAndNormalizeParams(ref Int32 Advance,ref Int32 StartPosition, String MethodName)
        {
            if(StartPosition == IRunner.CURRENT_POSITION) StartPosition = _position;
            if(StartPosition < _position) {
                String classname = Utilities.MakeClassCategoryName(GetType());
                throw new ArgumentException(nameof(Advance),
                        $"{classname}.{MethodName}:StartPosition value ({StartPosition}) is behind current runner Position({Position})");
            }
            if(Advance<0) {
                String classname = Utilities.MakeClassCategoryName(GetType());
                throw new ArgumentException(nameof(Advance),
                        $"{classname}.{MethodName}:Advance value ({Advance}) is negative");

            }
            if(StartPosition==_position && Advance == IRunner.DEFAULT_ADVANCE) Advance = 1;
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
            CompleteWaitingTasks();
        }

        void CompleteWaitingTasks()
        {
            TaskListItem? task_item;
            Int32 task_position;
            while(_waitList.TryPeek(out task_item, out task_position) && (Status.IsFinal() || task_position <= _progress)) {
                task_item.Registration?.Dispose();
                _waitList.Dequeue();
                if(task_item.Token.IsCancellationRequested) task_item!.TaskSourceToComplete?.TrySetCanceled();
                else task_item!.TaskSourceToComplete?.TrySetResult(
                    new RunnerResult<TResult>(_result, Status, task_position, Status == RunnerStatus.Failed ? Exception : null));
            }
        }

        void SessionTaskCompletionHandler(Task Antecedent)
        {
            lock (_lock) {
                IsBackgroundExecutionCompleted = true;
                switch(Antecedent.Status) {
                    case TaskStatus.RanToCompletion:
                        SetStatus(RunnerStatus.Complete);
                        _progress++;
                        if(Antecedent is Task<TResult> task_with_result) _result = task_with_result.Result;
                        break;
                    case TaskStatus.Faulted:
                        Exception? exception = Antecedent.Exception;
                        if(exception != null && exception is AggregateException aggregate_exception)
                            if(aggregate_exception.InnerExceptions.Count == 1) exception = aggregate_exception.InnerExceptions[0];
                        if(SetStatus(RunnerStatus.Failed)) Exception = exception;
                        break;
                    case TaskStatus:
                        SetStatus(RunnerStatus.Aborted);
                        break;
                    default:
                        //TODO Something went wrong
                }
                CompleteWaitingTasks();
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
