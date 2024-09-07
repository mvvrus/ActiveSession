namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //An interface to access internal functionality of the AcitiveSession class
    //TODO(future) One day I will invert dependency of ActiveSessionStore on ActiveSession using this class
    internal interface IActiveSessionInternal
    {
        //TODO Implement in ActiveSession
        //TODO Write tests for this ActiveSession functionality
        Task<Boolean> WaitAsync(Type ServiceType, TimeSpan Timeout, CancellationToken Token);
        void Release(Type ServiceType);
    }
}
