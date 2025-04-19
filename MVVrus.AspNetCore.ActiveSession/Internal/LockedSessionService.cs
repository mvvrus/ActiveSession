namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class LockedSessionService<TService> : ILockedSessionService<TService>
    {
        readonly ISessionServicesHelper? _servicesHelper;
        Int32 _disposed = 0;

        internal LockedSessionService(ISessionServicesHelper? ServicesHelper, TService? Service)
        {
            _servicesHelper = ServicesHelper;
            this.Service=Service;
        }

        public TService? Service { get; init; }

        public Boolean IsReallyLocked { get { return _servicesHelper!=null; } }

        public void Dispose()
        {
            if(Interlocked.Exchange(ref _disposed,1)==0) {
                _servicesHelper?.ReleaseService(typeof(TService));
            }
        }
    }
}
