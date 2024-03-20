using System.Collections.Concurrent;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.IRunner;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using System.Collections;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public abstract class EnumerableRunnerBase<TItem> : RunnerBase, IRunner<IEnumerable<TItem>>, IRunnerBackgroundProgress, IAsyncDisposable
    {
        //TODO Implement logging
        const string PARALLELISM_NOT_ALLOWED = "Parallel operations are not allowed.";

        readonly BlockingCollection<TItem> _queue;
        readonly QueueFacade _queueFacade;
        readonly int _defaultAdvance;
        Task? _disposeTask = null;
        List<TItem>? _stashedFetch = null;
        volatile TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? _waitingTaskSource = null;
        //Pseudo-lock to block parallel execution of GetRequiredAsync/GetAvailable methods,
        //The code using it just exits then the pseudo-lock cannot be acquired,
        int _busy;

        /// <value>
        /// TODO
        /// </value>
        protected internal IItemsQueueFacade<TItem> Queue { get => _queueFacade; }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Cts"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Logger"></param>
        /// <param name="Options"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="QueueSize"></param>
        protected EnumerableRunnerBase(
            CancellationTokenSource? Cts, Boolean PassCtsOwnership, RunnerId RunnerId, ILogger? Logger,
            IOptionsSnapshot<ActiveSessionOptions> Options, Int32? DefaultAdvance = null, Int32? QueueSize = null
        ) : this(Cts, PassCtsOwnership, RunnerId, Logger,
                DefaultAdvance ?? Options.Value.DefaultEnumerableAdvance, QueueSize ?? Options.Value.DefaultEnumerableQueueSize) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Cts"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Logger"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="QueueSize"></param>
        protected EnumerableRunnerBase(
            CancellationTokenSource? Cts, Boolean PassCtsOwnership, RunnerId RunnerId, ILogger? Logger,
            Int32 DefaultAdvance, Int32 QueueSize
        ) : base(Cts, PassCtsOwnership, RunnerId, Logger)
        {
            _queue = new BlockingCollection<TItem>(QueueSize);
            _defaultAdvance = DefaultAdvance;
            _queueFacade = new QueueFacade(this);
        }

        ///<inheritdoc/>
        public ValueTask DisposeAsync()
        {
            if(SetDisposed()) {
                //LogTrace DisposeAsync called
                _disposeTask = DisposeAsyncCore();
            }
            return _disposeTask!.IsCompleted ? ValueTask.CompletedTask : new ValueTask(_disposeTask!);
        }

        /// <inheritdoc/>
        public override RunnerStatus Status
        {
            get
            {
                RunnerStatus status = base.Status;
                if(!Disposed() && status == Stalled && (_queue.Count > 0 || _stashedFetch != null)) status = Progressed;
#if TRACE
#endif
                return status;
            }
        }

        ///<inheritdoc/>
        public RunnerResult<IEnumerable<TItem>> GetAvailable(Int32 Advance = int.MaxValue, Int32 StartPosition = -1, String? TraceIdentifier = null)
        {
            CheckDisposed();
#if TRACE
#endif
            if (!TryAcquirePseudoLock()) {
                ThrowInvalidParallelism();
            }
            List<TItem> result = new List<TItem>();
            try {
#if TRACE
#endif
                ProcessEnumParmeters(ref StartPosition, ref Advance, _defaultAdvance, nameof(GetAvailable), Logger);
                FetchAvailable(Advance, result);
            }
            catch(Exception) {
#if TRACE
#endif
                ReleasePseudoLock();
                throw;
            }
#if TRACE
#endif
            return FinishWithResult(result);
        }

        ///<inheritdoc/>
        public ValueTask<RunnerResult<IEnumerable<TItem>>> GetRequiredAsync(Int32 Advance = 0, CancellationToken Token = default, Int32 StartPosition = -1, String? TraceIdentifier = null)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier ?? "<unknown>";
#if TRACE
#endif
            if(!TryAcquirePseudoLock()) {
                ThrowInvalidParallelism();
            }
            try {
                RunnerResult<IEnumerable<TItem>> runner_result;
                List<TItem> result = new List<TItem>();
                Task<RunnerResult<IEnumerable<TItem>>> result_task;

                ProcessEnumParmeters(ref StartPosition, ref Advance, _defaultAdvance, nameof(GetRequiredAsync), Logger);
                Task<Boolean> startup_task=StartRunningAsync();
                if(startup_task.IsCompleted) {
                    //Background process initialization has been already done or completed synchronously
                    if(startup_task.IsCanceled) 
                        return new ValueTask<RunnerResult<IEnumerable<TItem>>>(
                            Task.FromCanceled<RunnerResult<IEnumerable<TItem>>>(new CancellationToken(true)));
                    if(startup_task.IsFaulted)
                        return new ValueTask<RunnerResult<IEnumerable<TItem>>>(
                            Task.FromException<RunnerResult<IEnumerable<TItem>>>(startup_task.Exception!.InnerExceptions[0]));
#if TRACE
#endif
                    if(FetchAvailable(Advance, result)) {
                        //Short path successfull: set correct Status
                        runner_result = FinishWithResult(result);
#if TRACE
#endif
                        return new ValueTask<RunnerResult<IEnumerable<TItem>>>(runner_result);
                    }
                    else {
                        //Come here if the short path failed: available data at current status cannot satisfy the request, so some async work is needed
                        _waitingTaskSource = new TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>();
                        result_task = _waitingTaskSource.Task;
                        AttacFetchResultProcessing(FetchRequiredAsync(Advance, result, Token), 
                            new Context(result, Advance, trace_identifier, Token));
                    }
                }
                else {
                    //Background process initialisation is required and have not been completed synchronously
#if TRACE
#endif
                    _waitingTaskSource = new TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>();
                    result_task = _waitingTaskSource.Task;
                    //TODO Releasepseudo-lock an reset _waitingTaskSource
                    startup_task.ContinueWith(SetCancelResult, TaskContinuationOptions.OnlyOnCanceled);
                    startup_task.ContinueWith(SetFailResult, TaskContinuationOptions.OnlyOnFaulted);
                    startup_task.ContinueWith(ContinueAsyncStartBackgroundProcessing,
                        new Context(result, Advance, trace_identifier, Token),
                        TaskContinuationOptions.OnlyOnRanToCompletion);
                }
#if TRACE
#endif
                return new ValueTask<RunnerResult<IEnumerable<TItem>>>(result_task);
            }
            catch(Exception) {
#if TRACE
#endif
                ReleasePseudoLock();
                throw;
            }
        }

        ///<inheritdoc/>
        ///<remarks>
        ///Retunrs total number of elements already fetched by background process in the Progress result field
        ///The EstimatedEnd result field will be left null until the runner background execution finishes.
        ///Otherwise it will contain the same value as Progress field
        ///</remarks>
        public (Int32 Progress, Int32? EstimatedEnd) GetProgress()
        {
            CheckDisposed();
            Int32 progress = _queueFacade.AddedCount;
            return (progress, (IsBackgroundExecutionCompleted ? progress : null));
        }

        /// <inheritdoc/>
        public Boolean IsBackgroundExecutionCompleted { get { CheckDisposed(); return _queue.IsAddingCompleted; } }

        ///<inheritdoc/>
        protected override void PreDispose()
        {
            base.PreDispose();
            TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? waiting_task_source = _waitingTaskSource;
            if(waiting_task_source!=null) {
                //TODO LogTrace
                waiting_task_source!.TrySetException(new ObjectDisposedException(DisposedObjectName()));
                ReleasePseudoLock();
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Disposing"></param>
        protected sealed override void Dispose(bool Disposing)
        {
            DisposeAsyncCore().Wait();
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        protected virtual Task DisposeAsyncCore()
        {
            _queue.Dispose();
            base.Dispose(true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected override void DoAbort()
        {
            base.DoAbort();
            if (!Disposed())try {
                _queue.CompleteAdding();
            }
            catch(ObjectDisposedException) { };
        }
        /// <summary>
        /// TODO
        /// </summary>
        protected internal override Task StartBackgroundExecutionAsync() { throw new NotImplementedException(); }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="MaxAdvance"></param>
        /// <param name="Result"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected internal abstract Task FetchRequiredAsync(Int32 MaxAdvance, List<TItem> Result, CancellationToken Token);

        /// <summary>
        /// TODO
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        protected void ThrowInvalidParallelism()
        {
            //TODO Log error
            throw new InvalidOperationException(PARALLELISM_NOT_ALLOWED);
        }

        void ContinueAsyncStartBackgroundProcessing(Task FetchTask, Object? Context)
        {
            Context context = (Context as Context) ?? throw new ArgumentException(nameof(Context));
            if(FetchAvailable(context.Advance, context.Accumulator)) {
                //Short path successfull: set correct Status
                FinishAndMakeResultBody(FetchTask, Context);
            }
            else {
                AttacFetchResultProcessing(FetchRequiredAsync(context.Advance, context.Accumulator, context.Token), context);
            }

        }

        void AttacFetchResultProcessing(Task FetchTask, Context Context)
        {
            FetchTask.ContinueWith(FinishAndMakeResultBody, Context, TaskContinuationOptions.OnlyOnRanToCompletion);
            FetchTask.ContinueWith(CancelResultBody, Context, TaskContinuationOptions.OnlyOnCanceled);
            FetchTask.ContinueWith(FailResultBody, Context, TaskContinuationOptions.OnlyOnFaulted);
        }

        void FailResultBody(Task FetchTask, Object? State)
        {
            Context state = (Context)(State ?? throw new ArgumentNullException(nameof(State)));
            //TODO LogTrace
            StashOrphannedData(state.Accumulator);
            SetFailResult(FetchTask);
        }

        void SetFailResult(Task Antecedent)
        {
            //TODO LogTrace
            TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? waitingTaskSource = _waitingTaskSource;
            _waitingTaskSource = null;
            ReleasePseudoLock();
            waitingTaskSource?.TrySetException(Antecedent.Exception!.InnerExceptions);
        }

        void CancelResultBody(Task _, Object? State)
        {
            Context state = (Context)(State ?? throw new ArgumentNullException(nameof(State)));
            //TODO LogTrace
            StashOrphannedData(state.Accumulator);
            SetCancelResult(_);
        }

        void SetCancelResult(Task _)
        {
            //TODO LogTrace
            TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? waitingTaskSource = _waitingTaskSource;
            _waitingTaskSource = null;
            ReleasePseudoLock();
            waitingTaskSource?.TrySetCanceled();
        }

        void FinishAndMakeResultBody(Task FetchTask, Object? State)
        {
            //We come here only if FetchTask is completed successfully
            Context state = (Context)(State ?? throw new ArgumentNullException(nameof(State)));
            //TODO LogTrace
            RunnerResult<IEnumerable<TItem>> result = MakeResultAndAdjustState(state.Accumulator);
            //TODO LogDebug result
            TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? waitingTaskSource = _waitingTaskSource;
            _waitingTaskSource = null;
            ReleasePseudoLock();
            waitingTaskSource?.TrySetResult(result);
            //TODO LogTrace
        }

        void StashOrphannedData(List<TItem> Data)
        {
            //TODO LogTrace
            //TODO Where to check: Debug.Assert(_stashedFetch==null);
            _stashedFetch = Data;
        }

        RunnerResult<IEnumerable<TItem>> FinishWithResult(List<TItem> ResultList)
        {
            RunnerResult<IEnumerable<TItem>> result = MakeResultAndAdjustState(ResultList);
            ReleasePseudoLock();
            //TODO LogDebug
            return result;
        }

        RunnerResult<IEnumerable<TItem>> MakeResultAndAdjustState(List<TItem> ResultList)
        {
            Position=Position+ResultList.Count;
            if (_queue.Count==0 && _queue.IsAddingCompleted) {
                RunnerStatus new_status = Exception==null ? Complete : Failed;
                //TODO LogTrace
                SetStatus(new_status);
            }
            RunnerResult<IEnumerable<TItem>> result = new RunnerResult<IEnumerable<TItem>>(ResultList, Status, Position, Status==Failed ? Exception : null);
            //TODO LogTrqce
            return result;
        }

        void ReleasePseudoLock()
        {
            Volatile.Write(ref _busy, 0);
        }

        Boolean TryAcquirePseudoLock()
        {
            return Interlocked.CompareExchange(ref _busy, 1, 0)==0;
        }

        void ProcessEnumParmeters(
            ref Int32 StartPosition,
            ref Int32 Advance,
            Int32 DefaultAdvance,
            String MethodName,
            ILogger? Logger = null)
        {
            String classname = GetType().FullName??"<unknown type>";
            if (StartPosition==CURRENT_POSITION)
                StartPosition=Position;
            if (StartPosition!=Position) {
                //TODO LogError
                throw new InvalidOperationException($"{classname}.{MethodName}: A start position requested ({StartPosition}) differs from the current one({Position})");
            }
            if (Advance==DEFAULT_ADVANCE)
                Advance=DefaultAdvance;
            if (Advance<=0) {
                //TODO LogError
                throw new InvalidOperationException($"{classname}.{MethodName}: Invalid advance value: {Advance}");
            }

        }

        internal Boolean FetchAvailable(Int32 MaxAdvance, List<TItem> Result)
        //Returns true if all available results fetched, internal access is added for testing purposes
        {
            TItem? item;

            if (Status.IsFinal())  return true;
            int fetched_count = Result.Count;
            if (_stashedFetch!=null) {
                //Fetch from orphanned results of previosly cancelled or failed  GetRequiredAsync
                int orphanned_count = _stashedFetch.Count;
                if (orphanned_count<=MaxAdvance-fetched_count) {
                    Result.AddRange(_stashedFetch);
                    fetched_count+=orphanned_count;
                    _stashedFetch=null;
                }
                else {
                    Result.AddRange(_stashedFetch!.GetRange(0, MaxAdvance-fetched_count));
                    _stashedFetch.RemoveRange(0, MaxAdvance - fetched_count);
                    fetched_count = MaxAdvance;
                }
            }
            //Fetch from current queue
            for (; fetched_count<MaxAdvance && _queue.TryTake(out item); fetched_count++)
                Result.Add(item);
            return fetched_count>=MaxAdvance || _queue.IsAddingCompleted && _queue.Count==0;
        }

        class Context
        {
            public List<TItem> Accumulator { get; init; }
            public String TraceIdentifier { get; init; }
            public Int32 Advance { get; init; }
            public CancellationToken Token { get; init; }
            public Context(List<TItem> Accumulator, Int32 Advance, String TraceIdentifier, CancellationToken Token)
            {
                this.Accumulator = Accumulator;
                this.TraceIdentifier = TraceIdentifier;
                this.Advance = Advance;
                this.Token = Token;
            }
        }

        class QueueFacade : IItemsQueueFacade<TItem>
        {
            EnumerableRunnerBase<TItem> _this;
            public Int32 AddedCount { get; private set; } = 0;

            public QueueFacade(EnumerableRunnerBase<TItem> This) { _this = This; }
            public Boolean IsAddingCompleted => _this._queue.IsAddingCompleted;
            public Int32 Count => _this._queue.Count;
            public void CompleteAdding() => _this._queue.CompleteAdding();
            public Boolean TryAdd(TItem Item, Int32 Timeout, CancellationToken Token)
            {
                Boolean result = _this._queue.TryAdd(Item, Timeout, Token);
                if(result) AddedCount= AddedCount + 1;
                return result;
            }
            public Boolean TryTake(out TItem Item) => _this._queue.TryTake(out Item!);
        }

    }
}
