namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class LockedSessionService<TService> : ILockedSessionService<TService>
    {
        //TODO Tests
        IActiveSessionInternal? _activeSession;

        internal LockedSessionService(IActiveSessionInternal? ActiveSession, TService? Service)
        {
            _activeSession = ActiveSession;
            this.Service=Service;
        }

        public TService? Service { get; init; }

        public Boolean IsReallyLocked { get { return _activeSession!=null; } }

        public void Dispose()
        {
            _activeSession?.Release(typeof(TService));
        }
    }
}
