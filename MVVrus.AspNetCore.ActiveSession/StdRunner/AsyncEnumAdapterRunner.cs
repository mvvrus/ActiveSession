using System.Collections;
using System.Collections.Concurrent;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.StdRunner.StdRunnerConstants;


namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class AsyncEnumAdapterRunner<TItem> : RunnerBase, IRunner<IEnumerable<TItem>>, IAsyncDisposable
    {
        readonly Action<Task<bool>> _itemActionDelegate;
        readonly Action<Task> _returnRestDelegate;

        readonly IAsyncEnumerable<TItem> _asyncSource;
        readonly int _defaultAdvance; 
        readonly bool _asyncEnumerableOwned; 
        readonly BlockingCollection<TItem> _queue;

        IAsyncEnumerator<TItem> _asyncEnumerator = null!;
        Context? _resultContext;
        volatile Task _taskChainTail;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="AsyncSource"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public AsyncEnumAdapterRunner(IAsyncEnumerable<TItem> AsyncSource, RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this(AsyncSource, true, null, true, null, null, false, RunnerId, LoggerFactory) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public AsyncEnumAdapterRunner(AsyncEnumAdapterParams<TItem> Params, RunnerId RunnerId, ILoggerFactory? LoggerFactory): 
            this(Params.Source,Params.PassSourceOnership,Params.CompletionTokenSource,Params.PassCtsOwnership,
                Params.DefaultAdvance,Params.EnumAheadLimit, Params.StartInConstructor, RunnerId, LoggerFactory) { }

        AsyncEnumAdapterRunner(
            IAsyncEnumerable<TItem> AsyncSource,
            Boolean PassSourceOnership,
            CancellationTokenSource? CompletionTokenSource,
            Boolean PassCtsOwnership,
            Int32? DefaultAdvance,
            Int32? EnumAheadLimit,
            Boolean StartInConstructor,
            RunnerId RunnerId, 
            ILoggerFactory? LoggerFactory):
            this(AsyncSource,PassSourceOnership,CompletionTokenSource,PassCtsOwnership,DefaultAdvance,EnumAheadLimit, 
                StartInConstructor, RunnerId,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(AsyncEnumAdapterRunner<TItem>))))
        {
            LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(GetType()));
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="AsyncSource"></param>
        /// <param name="PassSourceOnership"></param>
        /// <param name="CompletionTokenSource"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="EnumAheadLimit"></param>
        /// <param name="StartInConstructor"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Logger"></param>
        protected AsyncEnumAdapterRunner(
            IAsyncEnumerable<TItem> AsyncSource,
            Boolean PassSourceOnership,
            CancellationTokenSource? CompletionTokenSource,
            Boolean PassCtsOwnership,
            Int32? DefaultAdvance,
            Int32? EnumAheadLimit,
            Boolean StartInConstructor,
            RunnerId RunnerId,
            ILogger? Logger) : base(CompletionTokenSource, PassCtsOwnership, RunnerId, Logger)
        {
            _asyncSource = AsyncSource;
            _queue = new BlockingCollection<TItem>(EnumAheadLimit??ENUM_AHEAD_DEFAULT_LIMIT);
            _taskChainTail = Task.CompletedTask;
            _itemActionDelegate = ItemAction;
            _returnRestDelegate = ReturnRest;
            _defaultAdvance=DefaultAdvance??ENUM_DEFAULT_ADVANCE;
            _asyncEnumerableOwned=PassSourceOnership;
             if(StartInConstructor) StartSourceEnumerationIfNotStarted();
        }

        /// <inheritdoc/>
        public RunnerResult<IEnumerable<TItem>> GetAvailable(int StartPosition = -1, int Advance = int.MaxValue, string? TraceIdentifier = null)
        {
            CheckDisposed();
            if (State == NotStarted || State.IsFinal())
                return MakeRunnerEnumResult(new List<TItem>());
            Context? completed_context;
            //Acquire pseudo-lock
            if (Interlocked.CompareExchange(ref _resultContext, new Context(Advance), null) != null)
            {
                ThrowInvalidParallelism();
            }
            try
            {
                Utilities.ProcessEnumParmeters(ref StartPosition, ref Advance, this, _defaultAdvance, nameof(GetAvailable), Logger);
                _resultContext.CopyAvailable(_queue);
                TailorRunningState();
            }
            finally
            {
                completed_context = Interlocked.Exchange(ref _resultContext, null);
            }
            return MakeRunnerEnumResult(completed_context?.Result ?? throw new Exception("Something went wrong"));
        }

        /// <inheritdoc/>
        public ValueTask<RunnerResult<IEnumerable<TItem>>> GetRequiredAsync(int StartPosition, int Advance, string? TraceIdentifier = null, CancellationToken Token = default)
        {
            CheckDisposed();
            //Acquire pseudo-lock
            if (Interlocked.CompareExchange(ref _resultContext, new Context(Advance), null) != null)
            {
                ThrowInvalidParallelism();
            }
            try {
                Utilities.ProcessEnumParmeters(ref StartPosition, ref Advance, this, _defaultAdvance, nameof(GetRequiredAsync), Logger);
                StartSourceEnumerationIfNotStarted();
                //Try a short, synchrous path first: see if available state and data allows to satisfy the request 
                bool short_path_ok = _resultContext.CopyAvailable(_queue);
                if (!short_path_ok) {
                    //Come here if the short path failed: available data at current state cannot satisfy the request, so some async work needed
                    _resultContext.StartAccumulation();
                    return new ValueTask<RunnerResult<IEnumerable<TItem>>>(_resultContext.ResultTaskSource.Task);
                }
                //Short path successfull: set correct State
                TailorRunningState();
            }
            catch (Exception)
            {
                //Release the pseudo-lock if exception was thrown
                Interlocked.Exchange(ref _resultContext, null);
                throw;
            }
            //Continue the short path here: release pseudo-lock and return
            Context completed_context = Interlocked.Exchange(ref _resultContext, null) ?? throw new Exception("Something went wrong");
            return new ValueTask<RunnerResult<IEnumerable<TItem>>>(MakeRunnerEnumResult(completed_context.Result));
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (SetDisposed()) {
                await DisposeAsyncCore();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected override void DoAbort()
        {
            //The queue may be disposed already, if so - eat the exception thrown due to this
            try { _queue.CompleteAdding(); } catch (ObjectDisposedException) { }
            base.DoAbort(); //Now does nothing but things may be changed
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Disposing"></param>
        protected sealed override void Dispose(bool Disposing)
        {
            DisposeAsyncCore().AsTask().Wait();
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            Task task_chain_tail;
            DoAbort();
            do
            {
                task_chain_tail = _taskChainTail;
                await task_chain_tail;
            } while (_taskChainTail != task_chain_tail);
            if (_asyncEnumerator != null)
                await _asyncEnumerator.DisposeAsync();

            _queue.Dispose();
            if (_asyncEnumerableOwned)
            {
                IAsyncDisposable? async_disposable = _asyncSource as IAsyncDisposable;
                if (async_disposable != null) await async_disposable.DisposeAsync();
                else (_asyncSource as IDisposable)?.Dispose();
            }
            base.Dispose(true);
        }

        void StartSourceEnumerationIfNotStarted()
        {
            if (StartRunning()) {
                //Start _asyncEnumerable enumeration task chain
                _asyncEnumerator=_asyncSource.GetAsyncEnumerator(CompletionToken);
                _taskChainTail=_asyncEnumerator.MoveNextAsync().AsTask()
                    .ContinueWith(_itemActionDelegate, TaskContinuationOptions.RunContinuationsAsynchronously);
            }
        }

        RunnerResult<IEnumerable<T>> MakeRunnerEnumResult<T>(ICollection<T> Data)
        {
            Position += Data.Count;
            return new RunnerResult<IEnumerable<T>> { Result = Data, State = State, Position = Position, FailureException = Exception };
        }

        void ItemAction(Task<bool> NextStep)
        {
            bool result_ready = false;
            bool proceed = false;
            bool state_is_final = false;
            bool continue_enum = false;

            try
            {
                if (NextStep.IsCanceled) Abort();
                result_ready = state_is_final = State.IsFinal();
                if (NextStep.IsFaulted)
                {
                    Exception = NextStep.Exception;
                    _queue.CompleteAdding();
                }
                if (NextStep.IsCompletedSuccessfully)
                {
                    if (NextStep.Result && !state_is_final)
                    {
                        _queue.Add(_asyncEnumerator.Current);
                        SetState(Progressed);
                        proceed = true;
                    }
                    else
                    {
                        if (state_is_final) //The queue may be legally disposed already, if so - eat the exception thrown due to this
                            try { _queue.CompleteAdding(); } catch (ObjectDisposedException) { }
                        else
                            _queue.CompleteAdding();
                    }
                }

                if (proceed)
                {
                    _taskChainTail = _asyncEnumerator.MoveNextAsync().AsTask().ContinueWith(_itemActionDelegate);
                    continue_enum = true;
                }
            }
            catch (Exception e)
            {
                Exception = e;
                _queue.CompleteAdding();
            }

            Context? result_context = Volatile.Read(ref _resultContext);
            if (result_context != null)
            {
                if (result_context.IsReadyToAccumulate)
                {
                    if (!state_is_final) result_ready = _queue.Count > 0 && result_context.CopyAvailable(_queue);
                    if (result_ready)
                    {
                        Context completed_context = Interlocked.Exchange(ref _resultContext, null) ?? throw new Exception("Something went wrong");
                        TailorRunningState();
                        completed_context.ResultTaskSource.SetResult(MakeRunnerEnumResult(completed_context.Result));
                    }
                }
                else
                {
                    //Handle future IsReadyToAccumulate change if enumeration is done but some data may be available
                    if (!continue_enum)
                    {
                        //Come here only if _queue.CompleteAdding was called(TODO? Assert)
                        _taskChainTail = result_context!.AccumulationLatch.Task.ContinueWith(_returnRestDelegate);
                    }
                }
            }
        }

        void ReturnRest(Task Antecedent)
        {
            //The task should run only if _queue.CompleteAdding was called (TODO? Assert)
            if (!State.IsFinal()) _resultContext?.CopyAvailable(_queue); //Should always return true due to _queue.CompleteAdding was called
            if (_queue.Count <= 0) SetState(Exception == null ? Complete : Failed);
            Context completed_context = Interlocked.Exchange(ref _resultContext, null) ?? throw new Exception("Something went wrong");
            completed_context.ResultTaskSource.SetResult(MakeRunnerEnumResult(completed_context.Result));
        }

        void TailorRunningState()
        {
            bool result = _queue.IsAddingCompleted && _queue.Count <= 0;
            if (result)
            {
                SetState(Exception == null ? Complete : Failed);
            }
            else
            {
                SetState(_queue.Count > 0 ? Progressed : Stalled);
            }
        }

        void ThrowInvalidParallelism()
        {
            //TODO Log error
            throw new InvalidOperationException(PARALLELISM_NOT_ALLOWED);
        }

        class Context
        {
            public TaskCompletionSource<RunnerResult<IEnumerable<TItem>>> ResultTaskSource { get; init; }
            public TaskCompletionSource AccumulationLatch { get; init; }
            public int ItemsToGetCount { get; init; }
            public ICollection<TItem> Result { get => _fetchedItems; }
            public bool IsReadyToAccumulate { get { return Volatile.Read(ref _isReadyToAccumulate); } }

            public Context(int ItemsToGetCount)
            {
                ResultTaskSource = new TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>();
                AccumulationLatch = new TaskCompletionSource();
                this.ItemsToGetCount = ItemsToGetCount;
                _fetchedItems = new List<TItem>();
            }

            public int AddItem(TItem Item)
            {
                _fetchedItems.Add(Item);
                return _fetchedItems.Count;
            }

            public bool CopyAvailable(BlockingCollection<TItem> Queue)
            {
                for (int i = 0; i < ItemsToGetCount; i++)
                {
                    TItem? item;
                    if (!Queue.TryTake(out item)) break;
                    _fetchedItems.Add(item);
                }
                return _fetchedItems.Count >= ItemsToGetCount || Queue.IsAddingCompleted;
            }

            public void StartAccumulation()
            {
                Volatile.Write(ref _isReadyToAccumulate, true);
                AccumulationLatch.SetResult();
            }

            readonly List<TItem> _fetchedItems;
            bool _isReadyToAccumulate;
        }
    }
}
