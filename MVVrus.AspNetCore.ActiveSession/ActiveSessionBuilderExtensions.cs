using MVVrus.AspNetCore.ActiveSession.Internal;

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
        /// <returns>The <paramref name="Builder"/> parameter value to allow call chaining.</returns>
        /// <remarks>
        /// <para>
        /// The ActiveSession midleware adds an implementation of <see cref="IActiveSessionFeature"/> feature interface
        /// to the <see cref="HttpContext.Features"/> collection.
        /// </para>
        /// <para>
        /// The feature is added only if at list one runner factory service (<see cref="IRunnerFactory{TRequest, TResult}"/>) 
        /// was registered inthe application's service container via one of AddActiveSession extension methods defined in the 
        /// <see cref="ActiveSessionServiceCollectionExtensions"/> class.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder) 
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
                Builder.UseMiddleware<ActiveSessionMiddleware>();
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

    }
}
