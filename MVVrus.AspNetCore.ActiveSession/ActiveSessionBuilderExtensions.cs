using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Text.RegularExpressions;
using static System.Text.RegularExpressions.RegexOptions;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains extension methods used to configure middleware for ActiveSession feature
    /// </summary>
    public static class ActiveSessionBuilderExtensions
    {
        /// <summary>
        /// Adds the ActiveSession midleware to an application middleware pipeline to be built
        /// </summary>
        /// <param name="Builder">The application middleware pipeline builder</param>
        /// <param name="Filter">
        /// A predicate that is used to filter requests for which an active session feature will be available.
        /// Defaults to null that means the feature will be available for all requests
        /// </param>
        /// <returns>The <paramref name="Builder"/> parameter value to allow call chaining.</returns>
        /// <remarks>
        /// <para>
        /// The ActiveSession midleware adds an implementation of the active session feature (of type <see cref="IActiveSessionFeature"/>)
        /// to the <see cref="HttpContext.Features"/> collection of the request processing context. 
        /// If the <paramref name="Filter"/> predicate exists and is not null, 
        /// the feature will be added by the middleware only if an HTTP  request under processing passes the predicate.
        /// Otherwise the feature will be added to any request context.
        /// </para>
        /// <para>
        /// The middleware will be added to the pipeline only if at least one runner factory service (<see cref="IRunnerFactory{TRequest, TResult}"/>) 
        /// is registered in the application's service container via one of AddActiveSession extension methods defined in the 
        /// <see cref="ActiveSessionServiceCollectionExtensions"/> class.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder, Func<HttpContext, Boolean>? Filter=null ) 
        {
            ILogger? logger = Builder.
                                ApplicationServices.
                                GetService<ILoggerFactory>()?.
                                CreateLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
            #if TRACE
            logger?.LogTraceUseActiveSessions();
            #endif

            //Try to get IActiveSessionStore fro DI container to check if any of AddActiveSessions methods were ever called
            try {
                Builder.ApplicationServices.GetRequiredService<IActiveSessionStore>();
                Builder.UseMiddleware<ActiveSessionMiddleware>(Filter??ACCEPTALL_FILTER);
                logger?.LogInformationActiveSessionMiddlewareRegistered();
            }
            catch (Exception exception) {
                logger?.LogWarningAbsentFactoryInplementations(exception);
            }
            #if TRACE
            logger?.LogTraceUseActiveSessionsExit();
            #endif
            return Builder;
        }

        /// <summary>
        /// Adds the ActiveSession midleware with a <see cref="Regex"/>-based request path filter to an application middleware pipeline to be built
        /// </summary>
        /// <param name="Builder">The application middleware pipeline builder</param>
        /// <param name="Filter">Regular expression to wich an HTTP request path must match to pass a filter</param>
        /// <param name="TimeOut">Timeout for regular expression match operation. Use a value from configuration if omitted.</param>
        /// <returns>The <paramref name="Builder"/> parameter value to allow call chaining.</returns>
        /// <remarks>
        /// <para>
        /// The ActiveSession midleware adds an implementation of the active session feature (of type <see cref="IActiveSessionFeature"/>)
        /// to the <see cref="HttpContext.Features"/> collection of the request processing context. 
        /// The feature will be added by the middleware only if the path of an HTTP request under processing 
        /// mathces the <paramref name="Filter"/> regular expression.
        /// </para>
        /// <para>
        /// The middleware will be added to the pipeline only if at least one runner factory service (<see cref="IRunnerFactory{TRequest, TResult}"/>) 
        /// is registered in the application's service container via one of AddActiveSession extension methods defined in the 
        /// <see cref="ActiveSessionServiceCollectionExtensions"/> class.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder, String Filter, TimeSpan TimeOut=default)
        {
            if (TimeOut==default) 
                TimeOut=Builder.ApplicationServices.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.PathRegexTimeout;
            Regex path_matcher=new Regex(Filter, IgnoreCase | Compiled | CultureInvariant, TimeOut);
            Func<HttpContext, Boolean> filter = context => path_matcher.IsMatch(context.Request.Path); 
            return UseActiveSessions(Builder, filter);
        }

        internal static Func<HttpContext, Boolean> ACCEPTALL_FILTER = _ => true;

    }
}
