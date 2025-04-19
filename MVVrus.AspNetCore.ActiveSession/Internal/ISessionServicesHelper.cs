namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //An interface to access internal functionality of the AcitiveSession class
    internal interface ISessionServicesHelper
    {
        Task<Boolean> WaitForServiceAsync(Type ServiceType, TimeSpan Timeout, CancellationToken Token);
        void ReleaseService(Type ServiceType);
    }
}
