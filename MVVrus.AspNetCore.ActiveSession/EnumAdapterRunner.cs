using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using static MVVrus.AspNetCore.ActiveSession.ActiveSessionRunnerState;
using static MVVrus.AspNetCore.ActiveSession.IActiveSessionRunner;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Adapter class making possible to use any class implementing <see cref="IEnumerable{T}"/>interface as an ActiveSession runner
    /// </summary>
    internal class EnumAdapterRunner<TResult> : IActiveSessionRunner<IEnumerable<TResult>>, IDisposable, System.Runtime.CompilerServices.ICriticalNotifyCompletion
    {
        //TODO Implement logging
        const String PARALLELISM_NOT_ALLOWED = "Parallel operations are not allowed.";
        const Int32 LONG_DELAY = 300;

        readonly IEnumerable<TResult> _base;
        Boolean _disposed;
        Int32 _busy;
        CancellationTokenSource _completionTokenSource;
        BlockingCollection<TResult> _queue;
        Int32 _state;
        Object _lock;
        Exception? _failureException;

        [ActiveSessionConstructor]
        public EnumAdapterRunner(EnumAdapterParams<TResult> Params) 
        {
            _base=Params.AdapterBase??throw new ArgumentNullException("Params.AdapterBase");
            _lock=new Object();
            _completionTokenSource = new CancellationTokenSource();
            _queue = new BlockingCollection<TResult>(Params.Limit);
            //TODO continue implementation of the constructor
        }

        /// <inheritdoc/>
        public ActiveSessionRunnerState State { 
            get {
                CheckDisposed();
                ActiveSessionRunnerState state = (ActiveSessionRunnerState)Volatile.Read(ref _state);
                if (state==Stalled&&_queue.Count>0) return Progressed;
                else return state; 
            } 
        }

        /// <inheritdoc/>
        public Int32 Position { get; private set; }

        /// <inheritdoc/>
        public void Abort()
        {
            if (_disposed) return;
            Boolean state_changed=Interlocked.CompareExchange(ref _state, (Int32)Aborted, (Int32)Stalled)==(Int32)Stalled
                || Interlocked.CompareExchange(ref _state, (Int32)Aborted, (Int32)NotStarted)==(Int32)NotStarted;
            if (state_changed) SetRunnerCompletion(Aborted);
        }

        /// <inheritdoc/>
        public IChangeToken GetCompletionToken()
        {
            CheckDisposed();
            return new CancellationChangeToken(_completionTokenSource.Token);
        }

        /// <inheritdoc/>
        public ActiveSessionRunnerResult<IEnumerable<TResult>> GetAvailable(Int32 StartPosition, Int32 Advance = MAXIMUM_ADVANCE, String? TraceIdentifier=null)
        {
            CheckDisposed();
            ActiveSessionRunnerResult<IEnumerable<TResult>> result = default;
            if (Interlocked.CompareExchange(ref _busy, 1, 0)!=0) {
                ThrowInvalidParallelism();
            }
            try {
                List<TResult> result_list = new List<TResult>();
                for(Int32 i=0;i<Advance && _queue.Count>0 && Volatile.Read(ref _state)==(Int32)Stalled; i++) {
                    TResult? item;
                    if (_queue.TryTake(out item)) {
                        result_list.Add(item??throw new NullReferenceException());
                        Position+=1;
                    }
                }
                if (_queue.IsAddingCompleted && _queue.Count==0) {
                    SetRunnerCompletion(Complete);
                }
                result=MakeResult(result_list);
            }
            finally {
                Volatile.Write(ref _busy, 0);
            }
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask<ActiveSessionRunnerResult<IEnumerable<TResult>>> GetMoreAsync(
            Int32 StartPosition, 
            Int32 Advance, 
            String? TraceIdentifier = null, 
            CancellationToken Token = default
        )
        {
            CheckDisposed();
            ActiveSessionRunnerResult<IEnumerable<TResult>> result=default;
            if (Interlocked.CompareExchange(ref _busy, 1, 0)!=0) {
                ThrowInvalidParallelism();
            }
            try {
                if (StartPosition==CURRENT_POSITION) StartPosition=Position;
                if (StartPosition!=Position)
                    throw new InvalidOperationException("GetMoreAsync: a start position differs from the current one");
                StartSourceEnumerationIfNotStarted();
                List<TResult> result_list = new List<TResult>();

                for ( int i=0; i<Advance && Volatile.Read(ref _state)==(Int32)Stalled && !Token.IsCancellationRequested;i++) {
                    TResult? item;
                    if (_queue.TryTake(out item)) {
                        result_list.Add(item??throw new NullReferenceException());
                        Position+=1;
                    }
                    else if(_queue.IsAddingCompleted) {
                        SetRunnerCompletion(Complete);
                    }
                    else await this; //TODO Log trace
                }
                result=MakeResult(result_list);
            }
            finally {
                Volatile.Write(ref _busy, 0);
            }
            return result;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;
            lock (_lock) {
                if (_disposed)
                    return;
                _disposed=true;
                _queue.Dispose();
                _completionTokenSource.Dispose();
            }
        }

        void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName!);
        }

        void SetRunnerCompletion(ActiveSessionRunnerState CompletionState)
        {
            Boolean state_set = Interlocked.CompareExchange(ref _state, (Int32)CompletionState, (Int32)NotStarted)==(Int32)NotStarted
                || Interlocked.CompareExchange(ref _state, (Int32)CompletionState, (Int32)Stalled)==(Int32)Stalled;
            if(state_set) Task.Run(SignalCompletion);
        }

        void SignalCompletion()
        {
            if (_disposed) return; 
            //Try to acquire _busy pseudo-lock waiting for awhile on a SpinWait and repeat if not acquered this time
            SpinWait spin_waiter = new SpinWait();
            while (Interlocked.CompareExchange(ref _busy, 1, 0)!=0) {
                if (!spin_waiter.NextSpinWillYield) spin_waiter.SpinOnce();
                else {
                    //Pseudo-lock has been not acuired yet, plan to repeat this task after a delay
                    Task.Delay(LONG_DELAY).ContinueWith(_ => SignalCompletion());
                    return;
                }
            }
            //Come here if the pseudo-lock was acquired
            try { 
                //Perorm signalling via cancellation of  _completionTokenSource
                _completionTokenSource.Cancel();
            }
            finally {
                //Release the pseudo-lock
                Volatile.Write(ref _busy, 0);
            }
        }

        void ThrowInvalidParallelism()
        {
            //TODO Log error
            throw new InvalidOperationException(PARALLELISM_NOT_ALLOWED);
        }

        ActiveSessionRunnerResult<IEnumerable<TResult>> MakeResult(IEnumerable<TResult> ResultList)
        {
            return new ActiveSessionRunnerResult<IEnumerable<TResult>>(ResultList, State, Position,State==Failed?_failureException:null);
        }

        void StartSourceEnumerationIfNotStarted()
        {
            if (State!=NotStarted) return;
            Volatile.Write(ref _state, (Int32)Stalled);
            Task.Run(EnumerateSource);
        }

        void EnumerateSource()
        {
            CancellationToken completion_token = _completionTokenSource.Token;
            try {
                foreach (TResult item in _base) {
                    if (Volatile.Read(ref _state)!=(Int32)Stalled)
                        break;
                    if (_queue.TryAdd(item, -1, completion_token))
                        TryRunAwaitContinuation();
                }
                SetRunnerCompletion(Complete);
            }
            catch (Exception e) {
                _failureException=e;
                SetRunnerCompletion(Failed);
            }
            finally {
                _queue.CompleteAdding();
                TryRunAwaitContinuation();
            }
        }

        #region Await stuff
        WaitCallback? _continuation_callback;
        ManualResetEventSlim _complete_event = new ManualResetEventSlim(true);

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
            WaitCallback wait_callback = (_ => { _complete_event.Set(); continuation(); });
            _complete_event.Reset();
            try {
                if(Interlocked.CompareExchange(ref _continuation_callback, wait_callback, null)!=null) {
                    ThrowInvalidParallelism();
                }
            }
            catch {
                _complete_event.Set();
            }
            if (_queue.IsAddingCompleted) TryRunAwaitContinuation();
        }

        private void TryRunAwaitContinuation()
        {
            WaitCallback? continuation_callback = Interlocked.Exchange(ref _continuation_callback, null);
            if (continuation_callback!=null) {
                ThreadPool.QueueUserWorkItem(continuation_callback!);
            }
        }
        #endregion

    }
}
