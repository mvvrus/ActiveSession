using System.Collections.Concurrent;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.IRunner;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public abstract class EnumerableRunnerBase<TItem> : RunnerBase, IRunner<IEnumerable<TItem>>, IAsyncDisposable
    {
        //TODO Implement logging
        const string PARALLELISM_NOT_ALLOWED = "Parallel operations are not allowed.";

        readonly BlockingCollection<TItem> _queue;
        readonly int _defaultAdvance;
        Task? _disposeTask = null;
        List<TItem>? _orphannedFetch=null;
        //Pseudo-lock to block parallel execution of GetRequiredAsync/GetAvailable methods,
        //The code using it just exits then the pseudo-lock cannot be acquired,
        int _busy;

        /// <value>
        /// TODO
        /// </value>
        protected BlockingCollection<TItem> Queue { get => _queue; }

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
                DefaultAdvance??Options.Value.DefaultEnumerableAdvance, QueueSize?? Options.Value.DefaultEnumerableQueueSize) {}

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
        }

        ///<inheritdoc/>
        public ValueTask DisposeAsync()
        {
            if (SetDisposed()) {
                //LogTrace DisposeAsync called
                _disposeTask=DisposeAsyncCore();
            }
            return _disposeTask!.IsCompleted?ValueTask.CompletedTask:new ValueTask(_disposeTask!);
        }

        /// <inheritdoc/>
        public override RunnerStatus Status
        {
            get
            {
                RunnerStatus status = base.Status;
                if (status==Stalled && (_queue.Count>0 || _orphannedFetch!=null)) status=Progressed;
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
            RunnerResult<IEnumerable<TItem>> runner_result;
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
                ProcessEnumParmeters(ref StartPosition, ref Advance, _defaultAdvance, nameof(GetRequiredAsync), Logger);
                if(StartRunning()) StartSourceEnumerationIfNotStarted();
                //Try a short, synchrous path first: see if available status and data allows to satisfy the request 
                if (FetchAvailable(Advance, result)) {
                    //Short path successfull: set correct Status
                    runner_result=FinishWithResult(result);
#if TRACE
#endif
                    return new ValueTask<RunnerResult<IEnumerable<TItem>>>(runner_result);
                }
                else {
                    //Come here if the short path failed: available data at current status cannot satisfy the request, so some async work needed
                    Task<RunnerResult<IEnumerable<TItem>>> result_task;
                    if (Token.IsCancellationRequested) {
#if TRACE
#endif
                        result_task=Task.FromCanceled<RunnerResult<IEnumerable<TItem>>>(Token);
                    }
                    else {
#if TRACE
#endif
                        result_task=FetchRequiredAsync(Advance, result, Token).
                            ContinueWith(FinishAndMakeResultTaskBody, new Context(Advance, result));
                    }
                    ;
#if TRACE
#endif
                    return new ValueTask<RunnerResult<IEnumerable<TItem>>>(result_task);
                }
            }
            catch (Exception) { 
#if TRACE
#endif
                ReleasePseudoLock();
                throw;
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
        protected abstract void StartSourceEnumerationIfNotStarted();

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="MaxAdvance"></param>
        /// <param name="Result"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected abstract Task FetchRequiredAsync(Int32 MaxAdvance, List<TItem> Result, CancellationToken Token);

        /// <summary>
        /// TODO
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        protected void ThrowInvalidParallelism()
        {
            //TODO Log error
            throw new InvalidOperationException(PARALLELISM_NOT_ALLOWED);
        }

        RunnerResult<IEnumerable<TItem>> FinishWithResult(List<TItem> ResultList)
        {
            Position=Position+ResultList.Count;
            if (_queue.Count==0 && _queue.IsAddingCompleted) {
                RunnerStatus new_status = Exception==null ? Complete : Failed;
                //TODO LogTrace
                SetStatus(new_status);
            }
            RunnerResult<IEnumerable<TItem>> result = new RunnerResult<IEnumerable<TItem>>(ResultList, Status, Position, Status==Failed ? Exception : null);
            //TODO LogTrqce
            ReleasePseudoLock();
            //TODO LogDebug
            return result;
        }

        RunnerResult<IEnumerable<TItem>> FinishAndMakeResultTaskBody(Task FetchTask, Object? State)
        {
            Context state = (Context)(State??throw new ArgumentNullException(nameof(State)));
            //TODO LogTrace
            //TODO Where to check: Debug.Assert(FetchTask.IsCompleted);
            if (!FetchTask.IsCompletedSuccessfully) {
                //Stash fetched data of this unsuccessfull fetch
                //TODO LogTrace
                //TODO Where to check: Debug.Assert(_orphannedFetch==null);
                _orphannedFetch=state.Result;
            }
            try {
                FetchTask.GetAwaiter().GetResult(); //No wait because FetchTask is completed already but may throw
            }
            catch (Exception) {
                //TODO LogTrace
                ReleasePseudoLock();
                throw;
            }
            //We come here only if FetchTask is completed successfully
            return FinishWithResult(state.Result);
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

        Boolean FetchAvailable(Int32 MaxAdvance, List<TItem> Result)//Returns true if all available results fetched
        {
            TItem? item;

            if (Status.IsFinal())  return true;
            int fetched_count = Result.Count;
            if (_orphannedFetch!=null) {
                //Fetch from orphanned results of previosly cancelled or failed  GetRequiredAsync
                int orphanned_count = _orphannedFetch.Count;
                if (orphanned_count<=MaxAdvance-fetched_count) {
                    Result.AddRange(_orphannedFetch);
                    fetched_count+=orphanned_count;
                    _orphannedFetch=null;
                }
                else {
                    Result.AddRange(_orphannedFetch!.GetRange(0, MaxAdvance-fetched_count));
                    _orphannedFetch.RemoveRange(0, MaxAdvance - fetched_count);
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
            public int MaxCount { get; init; }
            public List<TItem> Result { get; init; }

            public Context(Int32 MaxCount, List<TItem> Result)
            {
                this.MaxCount=MaxCount;
                this.Result=Result;
            }
        }

    }
}
