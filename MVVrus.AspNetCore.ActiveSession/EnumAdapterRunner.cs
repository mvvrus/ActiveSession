using Microsoft.Extensions.Primitives;

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
        const String PARALLELISM_NOT_ALLOWED= "Parallel operations are not allowed.";

        [ActiveSessionConstructor]
        EnumAdapterRunner(EnumAdapterParams<TResult> Params) 
        {
            _base=Params.AdapterBase??throw new ArgumentNullException("Params.AdapterBase");
            _tokenSource = new CancellationTokenSource();
            //TODO
        }

        /// <inheritdoc/>
        public ActiveSessionRunnerState State { get; private set; }

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
            _disposed=true;
            _tokenSource.Dispose();
        }

        /// <inheritdoc/>
        public ActiveSessionRunnerResult<IEnumerable<TResult>> GetAvailable(Int32 StartPosition)
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
        public ValueTask<ActiveSessionRunnerResult<IEnumerable<TResult>>> GetMoreAsync(Int32 StartPosition, Int32 Advance, CancellationToken token = default)
        {
            CheckDisposed();
            if (Interlocked.CompareExchange(ref _busy, 1, 0)!=0) {
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

        void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName!);
        }

        Action? _continuation;
        public EnumAdapterRunner<TResult> GetAwaiter() { return this; }
        public Boolean IsCompleted { get; private set; }
        public TResult GetResult()
        {
            //TODO
            throw new NotImplementedException();
        }

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

        Boolean _isEnumStarted;

        void EnumerateSource()
        {
            if (_isEnumStarted) return;
            _isEnumStarted = true;
            //TODO State?
            foreach(TResult item in _base) {
                //TODO Process Abort() call
                //TODO Add item to the queue
                Action? continuation = Volatile.Read(ref _continuation);
                if (continuation!=null) {
                    Volatile.Write(ref _continuation, null);
                    ThreadPool.QueueUserWorkItem(_=>continuation!());
                }
            }
            //TODO Process completion
        }
        
    }
}
