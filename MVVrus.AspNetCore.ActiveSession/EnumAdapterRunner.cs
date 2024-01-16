using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static MVVrus.AspNetCore.ActiveSession.RunnerState;
using static MVVrus.AspNetCore.ActiveSession.IRunner;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Adapter class making possible to use any class implementing <see cref="IEnumerable{T}"/>interface as an ActiveSession runner
    /// </summary>
    internal class EnumAdapterRunner<TResult> : RunnerBase, IRunner<IEnumerable<TResult>>, IDisposable, ICriticalNotifyCompletion
    {
        //TODO Implement logging
        const String PARALLELISM_NOT_ALLOWED = "Parallel operations are not allowed.";
        const Int32 SIGNAL_COMPLETION_DELAY_MSEC = 300;

        readonly BlockingCollection<TResult> _queue;
        readonly Boolean _passSourceOnership;
        readonly Int32 _defaultAdvance;
        readonly ILogger? _logger;
        IEnumerable<TResult>? _source;
        Task? _enumTask;

        //Pseudo-lock to block parallel execution of GetRequiredAsync/GetAvailable methods,
        //The code using it just exits then the pseudo-lock cannot be acquired,
        Int32 _busy;

        Exception? _failureException;

        [ActiveSessionConstructor]
        public EnumAdapterRunner(EnumAdapterParams<TResult> Params, ILoggerFactory? LoggerFactory): 
            base(Params.CompletionTokenSource, Params.PassCtsOwnership)
        {
            _source=Params.Source??throw new ArgumentNullException( nameof(Params), "AdapterBase property cannot be null");
            _logger=LoggerFactory?.CreateLogger(LOGGING_CATEGORY_NAME);
#if TRACE
#endif
            _queue = new BlockingCollection<TResult>(Params.Limit);
            _passSourceOnership=Params.PassSourceOnership;
            _defaultAdvance=Params.Limit;
            //TODO LogDebug parameters passed

            _runAwaitContinuationDelegate=RunAwaitContinuation;
#if TRACE
#endif
        }

        /// <inheritdoc/>
        public override RunnerState State {
            //Contains (Int32)State with one exception: contains (Int32)Stalled when State may return (Int32)Progressed, see the getter code,
            //It is Int32, not ActiveSessionState because it to be accessed via Volatile/Interlocked methods
            get
            {
                RunnerState state = base.State;
                if (state==Stalled && _queue.Count>0) state=Progressed;
#if TRACE
#endif
                return state; 
            } 
        }

        /// <inheritdoc/>
        public RunnerResult<IEnumerable<TResult>> GetAvailable(Int32 StartPosition, Int32 Advance = MAXIMUM_ADVANCE, String? TraceIdentifier=null)
        {
            CheckDisposed();
#if TRACE
#endif
            RunnerResult<IEnumerable<TResult>> result = default;
            if (Interlocked.CompareExchange(ref _busy, 1, 0)!=0) {
                ThrowInvalidParallelism();
            }
            try {
#if TRACE
#endif
                List<TResult> result_list = new List<TResult>();
                for(Int32 i=0;i<Advance && _queue.Count>0 && base.State==Stalled; i++) {
#if TRACE
#endif
                    TResult? item;
                    if (_queue.TryTake(out item)) {
#if TRACE
#endif
                        result_list.Add(item??throw new NullReferenceException());
                        Position+=1;
                    }
                }
#if TRACE
#endif
                if (_queue.IsAddingCompleted && _queue.Count==0) {
#if TRACE
#endif
                    SetState(Complete);
                }
                result=MakeResult(result_list);
            }
            finally {
#if TRACE
#endif
                Volatile.Write(ref _busy, 0);
            }
#if TRACE
#endif
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask<RunnerResult<IEnumerable<TResult>>> GetRequiredAsync(
            Int32 StartPosition, 
            Int32 Advance, 
            String? TraceIdentifier = null, 
            CancellationToken Token = default
        )
        {
            CheckDisposed();
#if TRACE
#endif
            RunnerResult<IEnumerable<TResult>> result=default;
            if (Interlocked.CompareExchange(ref _busy, 1, 0)!=0) {
                ThrowInvalidParallelism();
            }
            try {
#if TRACE
#endif
                if (StartPosition==CURRENT_POSITION) StartPosition=Position;
                if (StartPosition!=Position)
                    //TODO LogError
                    throw new InvalidOperationException("GetRequiredAsync: a start position differs from the current one");
                if (Advance==DEFAULT_ADVANCE) Advance=_defaultAdvance;
                if(Advance<=0)
                    //TODO LogError
                    throw new InvalidOperationException($"GetRequiredAsync: Invalid Advance value: {Advance}");
#if TRACE
#endif
                StartSourceEnumerationIfNotStarted();
                List<TResult> result_list = new List<TResult>();
#if TRACE
#endif
                for ( int i=0; i<Advance && base.State==Stalled && !Token.IsCancellationRequested;i++) {
                    TResult? item;
                    if (_queue.TryTake(out item)) {
#if TRACE
#endif
                        result_list.Add(item??throw new NullReferenceException());
                        Position+=1;
                    }
                    else if (_queue.IsAddingCompleted) {
#if TRACE
#endif
                        SetState(_failureException==null?Complete:Failed);
                    }
                    else {
#if TRACE
#endif
                        await this;
                    }
                }
                result=MakeResult(result_list);
            }
            finally {
                Volatile.Write(ref _busy, 0);
#if TRACE
#endif
            }
#if TRACE
#endif
            return result;
        }

        protected sealed override void Dispose(Boolean Disposing)
        {
            DisposeAsyncCore().AsTask().Wait();
        }

        protected async virtual ValueTask DisposeAsyncCore()
        {
            if(_enumTask!=null) await _enumTask!;
            _queue.Dispose();
            base.Dispose(true);
        }

        public ValueTask DisposeAsync()
        {
            return  SetDisposed() ? DisposeAsyncCore() : ValueTask.CompletedTask;
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

        RunnerResult<IEnumerable<TResult>> MakeResult(IEnumerable<TResult> ResultList)
        {
            //LogDebug
            return new RunnerResult<IEnumerable<TResult>>(ResultList, State, Position,State==Failed?_failureException:null);
        }

        void StartSourceEnumerationIfNotStarted()
        {
            if (State!=NotStarted) return; //TODO use no synchronization for preliminary check
#if TRACE
#endif
            if(StartRunning()) _enumTask=Task.Run(EnumerateSource);
#if TRACE
#endif
        }

        void EnumerateSource()
        {
            CancellationToken completion_token = CompletionToken;
            if (_source==null) return;
            try {
                foreach (TResult item in _source!) {
#if TRACE
#endif
                    if (base.State!=Stalled) {  //TODO check for State.IsFinal()
#if TRACE
#endif
                        break;
                    }
                    if (_queue.TryAdd(item, -1, completion_token)) {
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
            catch (Exception e) {
                //LogError
                _failureException=e;
            }
            finally {
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

        public EnumAdapterRunner<TResult> GetAwaiter() { return this; }
        public Boolean IsCompleted { get {return _queue.Count>0;} }
        public void GetResult() { _complete_event.Wait(); }

        public void OnCompleted(Action continuation)
        { 
            Schedule(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Schedule(continuation);
        }

        private void Schedule(Action continuation)
        {
            _complete_event.Reset();
            try {
                if(Interlocked.CompareExchange(ref _continuation, continuation, null)!=null) {
                    ThrowInvalidParallelism();
                }
            }
            catch {
#if TRACE
#endif
                _complete_event.Set();
                //TODO the reaction for an error
            }
            if (_queue.IsAddingCompleted) {
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
            if (continuation!=null) {
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
