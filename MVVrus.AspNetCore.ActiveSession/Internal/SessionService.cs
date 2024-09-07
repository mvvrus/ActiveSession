namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class SessionService<TService> : ISessionService<TService>
    {
        public TService? Service { get; init; }

        public Boolean IsFromSession { get; init; }

        public SessionService(ActiveSessionRef SessionServicesRef)
        {
            IsFromSession=SessionServicesRef.IsFromSession;
            Service =SessionServicesRef.Services.GetService<TService>();
        }
    }
}
