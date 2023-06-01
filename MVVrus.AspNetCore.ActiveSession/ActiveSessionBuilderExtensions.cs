using MVVrus.AspNetCore.ActiveSession.Internal;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains extension methods used to configure middleware for ActiveSession feature
    /// </summary>
    public static class ActiveSessionBuilderExtensions
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder) 
        {
            //Try to get IActiveSessionStore fro DI container to check if any of AddActiveSessions methods were ever called
            Builder.ApplicationServices.GetRequiredService<IActiveSessionStore>();
            return Builder.UseMiddleware<ActiveSessionMiddleware>();
        }

    }
}
