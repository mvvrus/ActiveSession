namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //An interface to access internal functionality of the AcitiveSession class
    //(future) One day I will invert dependency of ActiveSessionStore on ActiveSession using this class
    internal interface IActiveSessionInternal
    {
        Task<Boolean> WaitForServiceAsync(Type ServiceType, TimeSpan Timeout, CancellationToken Token);
        void ReleaseService(Type ServiceType);
    }
}
