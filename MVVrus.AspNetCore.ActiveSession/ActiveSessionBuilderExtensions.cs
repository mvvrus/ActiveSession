using MVVrus.AspNetCore.ActiveSession.Internal;

namespace MVVrus.AspNetCore.ActiveSession
{
    public static class ActiveSessionBuilderExtensions
    {
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder) 
        {
            //Try to get IActiveSessionStore fro DI container to check if any of AddActiveSessions methods were ever called
            Builder.ApplicationServices.GetRequiredService<IActiveSessionStore>();
            return Builder.UseMiddleware<ActiveSessionMiddleware>();
        }

    }
}
