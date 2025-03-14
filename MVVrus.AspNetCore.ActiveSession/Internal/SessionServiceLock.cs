
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
                IActiveSessionServicesHelper locker = _activeSessionRef.ActiveSessionInternal 
                    ?? throw new NotImplementedException("Service locking is not implemented.");
                if(await locker!.WaitForServiceAsync(typeof(TService), Timeout, Token))
                    return new LockedSessionService<TService>(locker, _activeSessionRef.Services.GetService<TService>());
                else return null;
            }
            else
                return new LockedSessionService<TService>(null, _activeSessionRef.Services.GetService<TService>());
        }
    }
}
