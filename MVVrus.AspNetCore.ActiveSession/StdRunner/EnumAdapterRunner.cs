using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.StdRunner.StdRunnerConstants;
using Microsoft.Extensions.Logging;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// This adapter class makes possible to use any class implementing <see cref="IEnumerable{T}"/>interface as an ActiveSession runner
    /// </summary>
    public class EnumAdapterRunner<TItem> : RunnerBase, IRunner<IEnumerable<TItem>>, IDisposable, ICriticalNotifyCompletion
    {
        //TODO Implement logging
        const int SIGNAL_COMPLETION_DELAY_MSEC = 300;

        readonly BlockingCollection<TItem> _queue;
        readonly bool _passSourceOnership;
        readonly int _defaultAdvance;
        IEnumerable<TItem>? _source;
        Task? _enumTask;

        //Pseudo-lock to block parallel execution of GetRequiredAsync/GetAvailable methods,
        //The code using it just exits then the pseudo-lock cannot be acquired,
        int _busy;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public EnumAdapterRunner(IEnumerable<TItem> Source, RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this(Source,true,null,true,null,null,false,RunnerId, LoggerFactory) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public EnumAdapterRunner(EnumAdapterParams<TItem> Params, RunnerId RunnerId, ILoggerFactory? LoggerFactory) :
            this(Params.Source, Params.PassSourceOnership, Params.CompletionTokenSource, Params.PassCtsOwnership, 
                Params.DefaultAdvance, Params.EnumAheadLimit, Params.StartInConstructor, RunnerId, LoggerFactory) {}

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="PassSourceOnership"></param>
        /// <param name="CompletionTokenSource"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="EnumAheadLimit"></param>
        /// <param name="StartInConstructor"></param>
        /// <param name="RunnerId"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected EnumAdapterRunner(
            IEnumerable<TItem> Source,
            Boolean PassSourceOnership,
            CancellationTokenSource? CompletionTokenSource,
            Boolean PassCtsOwnership,
            Int32? DefaultAdvance,
            Int32? EnumAheadLimit,
            Boolean StartInConstructor,
            RunnerId RunnerId,
            ILoggerFactory? LoggerFactory) :
            base(CompletionTokenSource, PassCtsOwnership, RunnerId,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<TItem>))))
        {
            _source = Source ?? throw new ArgumentNullException(nameof(Source));
#if TRACE
#endif
            _queue = new BlockingCollection<TItem>(EnumAheadLimit??ENUM_AHEAD_DEFAULT_LIMIT); 
            _passSourceOnership = PassSourceOnership;
            _defaultAdvance = DefaultAdvance??ENUM_DEFAULT_ADVANCE; 
            //TODO LogDebug parameters passed

            _runAwaitContinuationDelegate = RunAwaitContinuation;
            if (StartInConstructor) StartSourceEnumerationIfNotStarted();
#if TRACE
#endif
        }

        /// <inheritdoc/>
        public override RunnerStatus Status
        {
            //Contains (Int32)Status with one exception: contains (Int32)Stalled when Status may return (Int32)Progressed, see the getter code,
            //It is Int32, not RunnerStatus because it to be accessed via Volatile/Interlocked methods
            get
            {
                RunnerStatus status = base.Status;
                if (status == Stalled && _queue.Count > 0) status = Progressed;
#if TRACE
#endif
                return status;
            }
        }

        /// <inheritdoc/>
        public RunnerResult<IEnumerable<TItem>> GetAvailable(Int32 Advance = IRunner.MAXIMUM_ADVANCE, Int32 StartPosition = IRunner.CURRENT_POSITION, string? TraceIdentifier=null)
        {
            CheckDisposed();
#if TRACE
#endif
            RunnerResult<IEnumerable<TItem>> result = default;
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            {
                ThrowInvalidParallelism();
            }
            try
            {
#if TRACE
#endif
                Utilities.ProcessEnumParmeters(ref StartPosition, ref Advance, this, _defaultAdvance, nameof(GetAvailable), Logger);
                List<TItem> result_list = new List<TItem>();
                for (int i = 0; i < Advance && _queue.Count > 0 && base.Status == Stalled; i++)
                {
#if TRACE
#endif
                    TItem? item;
                    if (_queue.TryTake(out item))
                    {
#if TRACE
#endif
                        result_list.Add(item ?? throw new NullReferenceException());
                        Position += 1;
                    }
                }
#if TRACE
#endif
                if (_queue.IsAddingCompleted && _queue.Count == 0)
                {
#if TRACE
#endif
                    SetStatus(Complete);
                }
                result = MakeResult(result_list);
            }
            finally
            {
#if TRACE
#endif
                Volatile.Write(ref _busy, 0);
            }
#if TRACE
#endif
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask<RunnerResult<IEnumerable<TItem>>> GetRequiredAsync(
            Int32 Advance = IRunner.DEFAULT_ADVANCE,
            CancellationToken Token = default,
            Int32 StartPosition = IRunner.CURRENT_POSITION,
            String? TraceIdentifier = null)
        {
            CheckDisposed();
#if TRACE
#endif
            RunnerResult<IEnumerable<TItem>> result = default;
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            {
                ThrowInvalidParallelism();
            }
            try
            {
#if TRACE
#endif
                Utilities.ProcessEnumParmeters(ref StartPosition, ref Advance, this, _defaultAdvance, nameof(GetRequiredAsync), Logger);
#if TRACE
#endif
                StartSourceEnumerationIfNotStarted();
                List<TItem> result_list = new List<TItem>();
#if TRACE
#endif
                for (int i = 0; i < Advance && base.Status == Stalled && !Token.IsCancellationRequested; i++)
                {
                    TItem? item;
                    if (_queue.TryTake(out item))
                    {
#if TRACE
#endif
                        result_list.Add(item ?? throw new NullReferenceException());
                        Position += 1;
                    }
                    else if (_queue.IsAddingCompleted)
                    {
#if TRACE
#endif
                        SetStatus(Exception == null ? Complete : Failed);
                    }
                    else
                    {
#if TRACE
#endif
                        await this;
                    }
                }
                result = MakeResult(result_list);
            }
            finally
            {
                Volatile.Write(ref _busy, 0);
#if TRACE
#endif
            }
#if TRACE
#endif
            return result;
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return SetDisposed() ? DisposeAsyncCore() : ValueTask.CompletedTask;
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
        protected async virtual ValueTask DisposeAsyncCore()
        {
            if (_enumTask != null) await _enumTask!;
            _queue.Dispose();
            base.Dispose(true);
        }


        void ReleaseSource()
        {
#if TRACE
#endif
            IDisposable? base_disposable = _passSourceOnership ? Interlocked.Exchange(ref _source, null) as IDisposable : null;
            base_disposable?.Dispose();
            //TODO
        }


        void ThrowInvalidParallelism()
        {
            //TODO Log error
            throw new InvalidOperationException(PARALLELISM_NOT_ALLOWED);
        }

        RunnerResult<IEnumerable<TItem>> MakeResult(IEnumerable<TItem> ResultList)
        {
            //LogDebug
            return new RunnerResult<IEnumerable<TItem>>(ResultList, Status, Position, Status == Failed ? Exception : null);
        }

        void StartSourceEnumerationIfNotStarted()
        {
            if (Status != NotStarted) return; //TODO use no synchronization for preliminary check
#if TRACE
#endif
            if (StartRunning()) _enumTask = Task.Run(EnumerateSource);
#if TRACE
#endif
        }

        void EnumerateSource()
        {
            CancellationToken completion_token = CompletionToken;
            if (_source == null) return;
            try
            {
                foreach (TItem item in _source!)
                {
#if TRACE
#endif
                    if (base.Status != Stalled)
                    {  //TODO check for Status.IsFinal()
#if TRACE
#endif
                        break;
                    }
                    if (_queue.TryAdd(item, -1, completion_token))
                    {
#if TRACE
#endif
                        TryRunAwaitContinuation();
                    }
                    else if (completion_token.IsCancellationRequested)
                        break;
                }
#if TRACE
#endif
            }
            catch (Exception e)
            {
                //LogError
                Exception = e;
            }
            finally
            {
#if TRACE
#endif
                _queue.CompleteAdding();
                ReleaseSource();
                TryRunAwaitContinuation();
            }
#if TRACE
#endif
        }

        #region Await stuff
        readonly Action<Action> _runAwaitContinuationDelegate;
        Action? _continuation;
        readonly ManualResetEventSlim _complete_event = new ManualResetEventSlim(true);

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public EnumAdapterRunner<TItem> GetAwaiter() { return this; }
        /// <summary>
        /// TODO
        /// </summary>
        public bool IsCompleted { get { return _queue.Count > 0; } }
        /// <summary>
        /// TODO
        /// </summary>
        public void GetResult() { _complete_event.Wait(); }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            Schedule(continuation);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="continuation"></param>
        public void UnsafeOnCompleted(Action continuation)
        {
            Schedule(continuation);
        }

        private void Schedule(Action continuation)
        {
            _complete_event.Reset();
            try
            {
                if (Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
                {
                    ThrowInvalidParallelism();
                }
            }
            catch
            {
#if TRACE
#endif
                _complete_event.Set();
                //TODO the reaction for an error
            }
            if (_queue.IsAddingCompleted)
            {
#if TRACE
#endif
                TryRunAwaitContinuation();
            }
#if TRACE
#endif
        }

        void TryRunAwaitContinuation()
        {
#if TRACE
#endif
            Action? continuation = Interlocked.Exchange(ref _continuation, null);
            if (continuation != null)
            {
#if TRACE
#endif
                ThreadPool.QueueUserWorkItem(_runAwaitContinuationDelegate, continuation, false);
            }
#if TRACE
#endif
        }

        void RunAwaitContinuation(Action Continuation)
        {
            _complete_event.Set();
            Continuation();
        }
        #endregion

    }
}
