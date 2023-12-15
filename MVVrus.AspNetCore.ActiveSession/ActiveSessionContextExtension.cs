namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This class contains extension methods for the <see cref="HttpContext"/> class associated with the ActiveSession feature
    /// </summary>
    public static class ActiveSessionContextExtension
    {
        /// <summary>
        /// Gives access to an ActiveSession for this request, if any/
        /// </summary>
        /// <param name="Context"><see cref="HttpContext"/>of the request.</param>
        /// <returns>Reference to ActiveSession associated to the request, null otherwise.</returns>
        public static IActiveSession? GetActiveSession(this HttpContext Context)
        {
            IActiveSession? active_session=Context.Features.Get<IActiveSessionFeature>()?.ActiveSession;
            if (active_session!=null&&active_session.IsAvailable) return active_session;
            else return null;
        }
    }
}
