namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionRef
    {
        //Provides an access to current ActiveSession object during resolving services from a DI container
        public virtual IServiceProvider Services { get; init;  }
        public virtual Boolean IsFromSession { get; init; }
        public virtual IActiveSession? ActiveSession { get; init; }
        public virtual ISessionServicesHelper? SessionServiceHelper { get; private set; }
        internal ActiveSessionRef() { Services=null!; } //For tests only
        public ActiveSessionRef(IHttpContextAccessor Accessor)
        {
            HttpContext context = Accessor.HttpContext??throw new InvalidOperationException("HttpContext is unaccessible");
            ActiveSession = context.GetActiveSession();
            if( !ActiveSession?.IsAvailable ?? false) ActiveSession = null;
            SessionServiceHelper = ActiveSession as ISessionServicesHelper;
            IsFromSession=ActiveSession != null;
            Services=IsFromSession ? ActiveSession!.SessionServices : context.RequestServices;
        }
    }
}
