namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class LockedSessionService<TService> : ILockedSessionService<TService>
    {
        readonly IActiveSessionServicesHelper? _activeSession;
        Int32 _disposed = 0;

        internal LockedSessionService(IActiveSessionServicesHelper? ActiveSession, TService? Service)
        {
            _activeSession = ActiveSession;
            this.Service=Service;
        }

        public TService? Service { get; init; }

        public Boolean IsReallyLocked { get { return _activeSession!=null; } }

        public void Dispose()
        {
            if(Interlocked.Exchange(ref _disposed,1)==0) {
                _activeSession?.ReleaseService(typeof(TService));
            }
        }
    }
}
