namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IRefDisposable:IDisposable
    {
        Int32 RefCount { get; }
        void AddRef();
        Boolean Release();
    }
}
