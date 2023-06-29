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
        readonly IEnumerable<TResult> _base;
        Boolean _disposed;
        Int32 _busy;
        CancellationTokenSource _tokenSource;
        BlockingCollection<TResult> _queue;
        volatile ActiveSessionRunnerState _state;
        Object _lock;
        const String PARALLELISM_NOT_ALLOWED= "Parallel operations are not allowed.";

        [ActiveSessionConstructor]
        public EnumAdapterRunner(EnumAdapterParams<TResult> Params) 
        {
            _base=Params.AdapterBase??throw new ArgumentNullException("Params.AdapterBase");
            _lock=new Object();
            _tokenSource = new CancellationTokenSource();
            _queue = new BlockingCollection<TResult>(Params.Limit);
            //TODO
        }

        /// <inheritdoc/>
        public ActiveSessionRunnerState State { 
            get {
                CheckDisposed();
                if (_state==Stalled&&_queue.Count>0) 
                    return Progressed;
                else return _state; 
            } 
        }

        /// <inheritdoc/>
        public Int32 Position { get; private set; }

        /// <inheritdoc/>
        public void Abort()
        {
            if (_disposed) return;
            //TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            lock(_lock) {
                if (_disposed) return;
                _disposed=true;
                _queue.Dispose();
                _tokenSource.Dispose();
            }
        }

        /// <inheritdoc/>
        public ActiveSessionRunnerResult<IEnumerable<TResult>> GetAvailable(Int32 StartPosition, String? TraceIdentifier=null)
        {
            CheckDisposed();
            if(Interlocked.CompareExchange(ref _busy, 1, 0)!=0) {
                ThrowInvalidParallelism();
            }
            try {
                //TODO
                throw new NotImplementedException();
            }
            finally {
                Volatile.Write(ref _busy, 0);
            }
        }

        /// <inheritdoc/>
        public IChangeToken GetCompletionToken()
        {
            CheckDisposed();
            return new CancellationChangeToken(_tokenSource.Token);
        }

        /// <inheritdoc/>
        public async ValueTask<ActiveSessionRunnerResult<IEnumerable<TResult>>> GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier = null, CancellationToken token = default)
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

                for ( int i=0; i<Advance && _state==Stalled;i++) {
                    //TODO Check for cancellation
                    TResult? item;
                    if (_queue.TryTake(out item)) {
                        result_list.Add(item??throw new NullReferenceException());
                        Position+=1;
                        //TODO
                    }
                    else if(_queue.IsAddingCompleted) {
                        //TODO
                    }
                    else await this; //TODO Log trace
                }
                result=new ActiveSessionRunnerResult<IEnumerable<TResult>>(result_list, State, Position);
            }
            finally {
                Volatile.Write(ref _busy, 0);
            }
            return result;
        }

        void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName!);
        }

        Action? _continuation;
        public EnumAdapterRunner<TResult> GetAwaiter() { return this; }
        public Boolean IsCompleted { get; private set; }
        public void GetResult() {}

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
            if(Interlocked.CompareExchange(ref _continuation, continuation, null)!=null) {
                ThrowInvalidParallelism();
            }
        }

        void ThrowInvalidParallelism()
        {
            //TODO Log error
            throw new InvalidOperationException(PARALLELISM_NOT_ALLOWED);
        }

        void StartSourceEnumerationIfNotStarted()
        {
            if (State!=NotStarted) return;
            _state=Stalled;
            //TODO Implement cancellation
            Task.Run(EnumerateSource);
        }

        void EnumerateSource()
        {
            foreach (TResult item in _base) {
                //TODO Process Abort() call i.e. implement cancellation
                //TODO Add item to the queue
                _queue.Add(item);
                Action? continuation = Volatile.Read(ref _continuation);
                if (continuation!=null) {
                    Volatile.Write(ref _continuation, null);
                    ThreadPool.QueueUserWorkItem(_=>continuation!());
                }
            }
            //TODO Process completion
            _queue.CompleteAdding();
        }
        
    }
}
