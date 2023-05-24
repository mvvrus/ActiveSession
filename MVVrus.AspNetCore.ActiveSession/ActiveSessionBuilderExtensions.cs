using System.Runtime.CompilerServices;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MVVrus.AspNetCore.ActiveSession.Internal;

namespace MVVrus.AspNetCore.ActiveSession
{
    public static class ActiveSessionBuilderExtensions
    {
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder) 
        {
            //TODO
            //Check if any of AddActiveSessions method ever called
            return Builder.UseMiddleware<ActiveSessionMiddleware>();
        }

    }
}
