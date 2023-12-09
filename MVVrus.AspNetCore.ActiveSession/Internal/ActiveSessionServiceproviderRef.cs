namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionServiceProviderRef
    {
        public virtual IServiceProvider SessionServices { get; init;  }
        public virtual Boolean IsFromSession { get; init; }
        public ActiveSessionServiceProviderRef(IHttpContextAccessor Accessor)
        {
            HttpContext context = Accessor.HttpContext??throw new InvalidOperationException("HttpContext is unaccessible");
            IActiveSession? active_session = context.Features.Get<IActiveSessionFeature>()?.ActiveSession;
            IsFromSession=active_session != null && active_session.IsAvailable;
            SessionServices=IsFromSession?active_session!.SessionServices:context.RequestServices;
        }
    }
}
