namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionRef
    {
        public virtual IServiceProvider Services { get; init;  }
        public virtual Boolean IsFromSession { get; init; }
        internal ActiveSessionRef() { Services=null!; } //For tests only
        public ActiveSessionRef(IHttpContextAccessor Accessor)
        {
            HttpContext context = Accessor.HttpContext??throw new InvalidOperationException("HttpContext is unaccessible");
            IActiveSession? active_session = context.GetActiveSession();
            IsFromSession=active_session != null && active_session!.IsAvailable;
            Services=IsFromSession ? active_session!.SessionServices : context.RequestServices;
        }
    }
}
