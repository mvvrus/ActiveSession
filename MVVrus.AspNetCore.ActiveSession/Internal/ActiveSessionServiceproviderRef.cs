namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionServiceProviderRef
    {
        public virtual IServiceProvider Services { get; init;  }
        public virtual Boolean IsFromSession { get; init; }
        internal ActiveSessionServiceProviderRef() { Services=null!; } //For tests only
        public ActiveSessionServiceProviderRef(IHttpContextAccessor Accessor)
        {
            HttpContext context = Accessor.HttpContext??throw new InvalidOperationException("HttpContext is unaccessible");
            IActiveSession? active_session = context.GetActiveSession();
            IsFromSession=active_session != null;
            Services=active_session?.SessionServices??context.RequestServices;
        }
    }
}
