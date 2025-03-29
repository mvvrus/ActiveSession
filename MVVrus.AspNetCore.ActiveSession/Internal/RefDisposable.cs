namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class RefDisposable: IRefDisposable
    {
        Int32 _disposedValue = 0;
        Int32 _refCount = 0;

        public Int32 RefCount => Volatile.Read(ref _refCount);
        public Boolean IsDisposed => Volatile.Read(ref _disposedValue)!=0;

        public void AddRef()
        {
            if(Volatile.Read(ref _disposedValue)>0) throw new ObjectDisposedException(nameof(ActiveSessionGroup));
            Interlocked.Increment(ref _refCount);
        }

        public Boolean Release()
        {
            if(Volatile.Read(ref _disposedValue)>0) return false;
            Boolean result = Interlocked.Decrement(ref _refCount)==0;
            if(result) Dispose();
            return result;
        }

        //Here is a difference with common Disposable pattern: this method is called only once, during actual disposal
        protected virtual void Dispose(Boolean Disposing) {}

        public void Dispose()
        {
            if(Interlocked.Exchange(ref _disposedValue, 1)==0) {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

    }
}
