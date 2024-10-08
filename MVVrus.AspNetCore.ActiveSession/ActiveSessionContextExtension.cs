﻿using MVVrus.AspNetCore.ActiveSession.Internal;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This class contains extension methods for the <see cref="HttpContext"/> class associated with the ActiveSession feature
    /// </summary>
    public static class ActiveSessionContextExtension
    {
        /// <summary>
        /// Gives access to an Active Session for this request, if any.
        /// </summary>
        /// <param name="Context"><see cref="HttpContext"/>of the request.</param>
        /// <returns>
        /// Reference of type <see cref="IActiveSession"/> to an active session associated with the request if available, 
        /// or to a dummy active session object which <see cref="ILocalSession.IsAvailable"/> property containing false
        /// </returns>
        public static IActiveSession GetActiveSession(this HttpContext Context)
        {
            IActiveSession? active_session=Context.Features.Get<IActiveSessionFeature>()?.ActiveSession;
            if (active_session!=null&&active_session.IsAvailable) return active_session;
            else return ActiveSessionFeature.DummySession;
        }

        /// <summary>
        /// Gives access to a Local Session for this request, if any.
        /// </summary>
        /// <param name="Context"><see cref="HttpContext"/>of the request.</param>
        /// <returns>
        /// Reference of type <see cref="ILocalSession"/> to a local session associated with the request if available, 
        /// or to a dummy local session object which <see cref="ILocalSession.IsAvailable"/> property containing false.
        /// </returns>
        public static ILocalSession GetLocalSession(this HttpContext Context)
        {
            ILocalSession? local_session = Context.Features.Get<IActiveSessionFeature>()?.LocalSession;
            if(local_session!=null&&local_session.IsAvailable) return local_session;
            else return ActiveSessionFeature.DummySession;
        }
    }
}
