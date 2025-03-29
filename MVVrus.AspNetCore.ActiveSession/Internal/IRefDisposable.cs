namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IRefDisposable:IDisposable
    {
        Int32 RefCount { get; }
        Boolean IsDisposed { get; }
        void AddRef();
        Boolean Release();
    }
}
