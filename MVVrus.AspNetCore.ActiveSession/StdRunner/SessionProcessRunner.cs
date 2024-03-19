using System;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// TODO                                                                                     
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class SessionProcessRunner<TResult> : RunnerBase, IRunner<TResult>, IRunnerBackgroundProgress
    {
        //Action<IProgressSetter<TResult>, CancellationToken> _procToRun;
        TResult _result=default!;
        Int32 _progress = 0;
        Int32? _estimatedEnd=null;
        Object _lock = new Object();
        Int32 _position = 0;
        PriorityQueue<TaskListItem, Int32> _waitList = new PriorityQueue<TaskListItem, Int32>();
        readonly Func<IRunnerProgressSetter<TResult>, CancellationToken, Task> _taskToRun;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="TaskBody"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Logger"></param>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            Func<IRunnerProgressSetter<TResult>, CancellationToken, TResult> TaskBody, RunnerId RunnerId, ILogger? Logger) :
            this(MakeTaskToRun(TaskBody), RunnerId, Logger) {}


        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="TaskBody"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Logger"></param>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            Action<IRunnerProgressSetter<TResult>, CancellationToken> TaskBody, RunnerId RunnerId, ILogger? Logger):
            this(MakeTaskToRun(TaskBody), RunnerId,Logger) {}

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="TaskToRun"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Logger"></param>
        protected SessionProcessRunner(
            Func<IRunnerProgressSetter<TResult>,CancellationToken,Task> TaskToRun, RunnerId RunnerId, ILogger? Logger) 
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
            Task t = _taskToRun(new ProgressSetter(this), CompletionToken).ContinueWith(SessionTaskCompletionHandler);
            if(t.Status == TaskStatus.Created) try { t.Start(); } catch(TaskSchedulerException) { }
        }

        static Action<Object?> MakeTaskToRunBody(
            Action<IRunnerProgressSetter<TResult>, CancellationToken> TaskBody)
        {
            return State=>TaskBody(
                ((ValueTuple<IRunnerProgressSetter<TResult>, CancellationToken>)State!).Item1,
                ((ValueTuple<IRunnerProgressSetter<TResult>, CancellationToken>)State!).Item2);
        } 

        static Func<IRunnerProgressSetter<TResult>, CancellationToken, Task> MakeTaskToRun(
            Action<IRunnerProgressSetter<TResult>, CancellationToken> TaskBody)
        {
            return (ProgressSetter,Token)=>new Task(MakeTaskToRunBody(TaskBody),(ProgressSetter, Token),Token);
        }

        static Action<Object?> MakeTaskToRunBody(
            Func<IRunnerProgressSetter<TResult>, CancellationToken, TResult> TaskBody)
        {
            return State =>
            {
                TResult result = TaskBody(
                    ((ValueTuple<IRunnerProgressSetter<TResult>, CancellationToken>)State!).Item1,
                    ((ValueTuple<IRunnerProgressSetter<TResult>, CancellationToken>)State!).Item2);
                ((ValueTuple<IRunnerProgressSetter<TResult>, CancellationToken>)State!).Item1.SetResult(result);
            };
        }

        static Func<IRunnerProgressSetter<TResult>, CancellationToken, Task> MakeTaskToRun(
            Func<IRunnerProgressSetter<TResult>, CancellationToken,TResult> TaskBody)
        {
            return (ProgressSetter, Token) => new Task(MakeTaskToRunBody(TaskBody), (ProgressSetter, Token), Token);
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
                if (StartPosition==IRunner.CURRENT_POSITION)  StartPosition=_position;
                if (StartPosition<_position) {
                    String classname = GetType().FullName??"<unknown type>";
                    throw new InvalidOperationException(
                            $"{classname}.{nameof(GetAvailable)}:Start positionfor the operation ({StartPosition}) is behind current runner Position({Position})");
                }
                Int32 max_advance = _progress-StartPosition;
                RunnerStatus new_status;
                if(max_advance<Advance) {
                    new_status=RunnerStatus.Stalled;
                    _position=_progress;
                }
                else {
                    new_status=RunnerStatus.Progressed;
                    _position+=Advance;
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
                if(StartPosition == IRunner.CURRENT_POSITION) {
                    new_position = Position + Advance == IRunner.DEFAULT_ADVANCE ? 1 : Advance;
                }
                else {
                    new_position = StartPosition + Advance == IRunner.DEFAULT_ADVANCE ? 0 : Advance;
                }
                if(new_position <= _position) {
                    String classname = GetType().FullName ?? "<unknown type>";
                    throw new InvalidOperationException(
                            $"{classname}.{nameof(GetRequiredAsync)}:End position for the operation ({new_position}) is behind current runner Position({Position})");
                }
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

            }        }

        /// <inheritdoc/>
        public (Int32 Progress, Int32? EstimatedEnd) GetProgress()
        {
            lock(_lock) return (_progress, _estimatedEnd);
        }


        /// <inheritdoc/>
        public override Int32 Position { get => _position;} //TODO Volatile.Read?

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
                RunnerStatus status = Antecedent.Status switch
                {
                    TaskStatus.RanToCompletion => RunnerStatus.Complete,
                    TaskStatus.Faulted => RunnerStatus.Failed,
                    TaskStatus.Canceled => RunnerStatus.Aborted,
                    _ => throw new InvalidOperationException()
                };
                if(SetStatus(status) && status == RunnerStatus.Failed) Exception = Antecedent.Exception;
                CompleteWaitingTasks();
            }
        }

        class ProgressSetter : IRunnerProgressSetter<TResult>
        {
            SessionProcessRunner<TResult> _host;

            public ProgressSetter(SessionProcessRunner<TResult> Host)
            {
                _host=Host;
            }

            public void SetProgress(TResult Result, Int32? EstimatedEnd)
            {
                lock(_host._lock) {
                    _host._result=Result;
                    _host._estimatedEnd=EstimatedEnd;
                    _host.AdvanceProgress();
                }
            }

            public void SetResult(TResult Result)
            {
                lock(_host._lock) {
                    _host._result = Result;
                    _host._estimatedEnd = _host._position+1;
                    _host.AdvanceProgress();
                }

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
