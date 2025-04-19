
namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class SessionServiceLock<TService> : ISessionServiceLock<TService>
    {
        ActiveSessionRef _activeSessionRef;

        public SessionServiceLock(ActiveSessionRef ActiveSessionRef) 
        {
            _activeSessionRef = ActiveSessionRef;
        }

        public async Task<ILockedSessionService<TService>?> AcquireAsync(TimeSpan Timeout, CancellationToken Token)
        {
            if(_activeSessionRef.IsFromSession) {
                ISessionServicesHelper locker = _activeSessionRef.SessionServiceHelper
                    ?? throw new NotImplementedException("Service locking is not implemented or session accessor has not been initialized.");
                if(await locker!.WaitForServiceAsync(typeof(TService), Timeout, Token))
                    return new LockedSessionService<TService>(locker, _activeSessionRef.Services.GetService<TService>());
                else return null;
            }
            else
                return new LockedSessionService<TService>(null, _activeSessionRef.Services.GetService<TService>());
        }
    }
}
