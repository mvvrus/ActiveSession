namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionService<TService> : IActiveSessionService<TService>
    {
        public TService? Service { get; init; }

        public Boolean IsFromSession { get; init; }

        public ActiveSessionService(ActiveSessionServiceProviderRef SessionServicesRef)
        {
            IsFromSession=SessionServicesRef.IsFromSession;
            Service =SessionServicesRef.Services.GetService<TService>();
        }
    }
}
