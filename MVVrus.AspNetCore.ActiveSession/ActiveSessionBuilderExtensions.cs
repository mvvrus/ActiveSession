using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Collections.Generic;
using System;
using System.Linq;
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
        /// Adds the ActiveSession midleware to an application middleware pipeline along with a filter defining a scope of HTTP requests, 
        /// for which an active session will be available.
        /// </summary>
        /// <param name="Builder">The application middleware pipeline builder</param>
        /// <param name="Filter">
        /// A reference to an object instance implementing the <see cref="IMiddlewareFilterSource"/> interface
        /// that is used to filter requests for which an active session feature will be available 
        /// and specify the suffix to be added to an <see cref="IActiveSession.Id">Id</see> property 
        /// of active sessions assigned to requests that falls into the scope of the filter set by this method.
        /// Defaults to null that means the feature will be available for all requests.
        /// </param>
        /// <returns>The <paramref name="Builder"/> parameter value to allow call chaining.</returns>
        /// <remarks>
        ///  <toinherit>
        /// <para>
        /// The ActiveSession midleware adds an implementation of the active session feature 
        /// (available through <see cref="IActiveSessionFeature"/>) interface
        /// to the <see cref="HttpContext.Features"/> collection in the request context. 
        /// This interface makes an active session object available for a request handler that uses the context.
        /// </para>
        /// <para>
        /// If a filter is specified, it defines a scope of requests for which a feature will be added to their contexts.
        /// Not specifying a filter means that an active session will be available for handlers of all requests.
        /// UseActiveSessions methods may be called multiple times to add different filters for different scopes.
        /// All of these calls result in addition of  a single ActiveSession middleware instance to the pipeline.
        /// This instance passes incoming requests to all added filters in order in which they have been added.
        /// If no filter accepts a request, a feature will not be added to the request context.
        /// </para>
        /// <para>
        /// The middleware can be added to the pipeline only if at least one runner factory service (<see cref="IRunnerFactory{TRequest, TResult}"/>) 
        /// is registered in the application's service container via one of AddActiveSession extension methods defined in the 
        /// <see cref="ActiveSessionServiceCollectionExtensions"/> class.
        /// </para>
        ///  </toinherit>
        /// <para>
        /// This overload of the UseActiveSession method uses an <see cref="IMiddlewareFilterSource"/> interface as a filter to define 
        /// will a request fall into its scope and, if so, which suffix will be used to construct an identifier of an active session 
        /// available to the request handler.
        /// If the <paramref name="Filter"/> is omitted or is <see langword="null"/>, the feature will be added 
        /// to request contexts for all request handlers. 
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder, IMiddlewareFilterSource? Filter = null)
        {
            ILogger? logger = Builder.
                                ApplicationServices.
                                GetService<ILoggerFactory>()?.
                                CreateLogger(ActiveSessionConstants.INIT_CATEGORY_NAME);
            #if TRACE
            logger?.LogTraceUseActiveSessions();
            #endif

            ActiveSessionMiddleware.MiddlewareParam middleware_param;
            if (Builder.Properties.ContainsKey(ACTIVESESSION_PROPERTYNAME)) {
                middleware_param=(ActiveSessionMiddleware.MiddlewareParam)Builder.Properties[ACTIVESESSION_PROPERTYNAME]!;
                #if TRACE
                logger?.LogTraceUseActiveSessionExtractExistingParams();
                #endif
            } 
            else {
                middleware_param=new ActiveSessionMiddleware.MiddlewareParam();
                #if TRACE
                logger?.LogTraceUseActiveSessionCreateNewParams();
                #endif
                try {
                    //Try to get IActiveSessionStore from DI container to check if any of AddActiveSessions methods were ever called
                    Builder.ApplicationServices.GetRequiredService<IActiveSessionStore>();
                    Builder.Properties.Add(ACTIVESESSION_PROPERTYNAME, middleware_param);
                    Builder.UseMiddleware<ActiveSessionMiddleware>(middleware_param);
                    logger?.LogInformationActiveSessionMiddlewareRegistered();
                }
                catch (InvalidOperationException exception) {
                    logger?.LogWarningAbsentFactoryInplementations(exception);
                }
            }
            if (Filter==null) {
                #if TRACE
                logger?.LogTraceUseActiveSessionParamsMarkCatchAll();
                #endif
                middleware_param.AcceptAll=true;
            }
            else {
                #if TRACE
                logger?.LogTraceUseActiveSessionParamsAddFilter();
                #endif
                middleware_param.Filters.Add(Filter);
            }
            #if TRACE
            logger?.LogTraceUseActiveSessionsExit();
            #endif
            return Builder;
        }

        /// <summary>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/summary' />
        /// </summary>
        /// <param name="Builder">
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/param[@name="Builder"]' />
        /// </param>
        /// <param name="Filter">
        /// A predicate that is used to filter requests for which an active session feature will be available.
        /// </param>
        /// <returns> <inheritdoc cref = "UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/returns' /> </returns>
        /// <remarks>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/remarks/toinherit' />
        /// <para>
        /// <filter>
        /// This UseActiveSessions method overload uses the predicate (a delegate returning a <see langword="bool"/> result) 
        /// to decide which requests fall into its scope 
        /// </filter>
        /// <suffix>
        /// and defines no suffix to be added to an active session identifier.
        /// </suffix>
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder, Func<HttpContext, Boolean> Filter)
        {
            return Builder.UseActiveSessions((SimplePredicateFilterSource)Filter);
        }

        /// <summary>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/summary' />
        /// The filter checks the request path against a <see cref="Regex">regular expression</see> passed as a parameter.
        /// </summary>
        /// <param name="Builder">
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/param[@name="Builder"]' />
        /// </param>
        /// <param name="Filter">String with a regular expression to wich an HTTP request path must match to pass a filter</param>
        /// <param name="TimeOut">Timeout for the regular expression match operation. Use a value from the configuration if omitted.</param>
        /// <returns> <inheritdoc cref = "UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/returns' /> </returns>
        /// <remarks>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/remarks/toinherit' />
        /// <para>
        /// <filter>
        /// This UseActiveSessions method overload uses the request path match 
        /// with the regular expression specified via <paramref name="Filter"/> parameter to decide which requests fall into its scope 
        /// </filter>
        ///  <inheritdoc cref="UseActiveSessions(IApplicationBuilder, Func{HttpContext, bool})" path="/remarks/para/suffix"/>
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder, String Filter, TimeSpan TimeOut=default)
        {
            if (TimeOut==default) 
                TimeOut=Builder.ApplicationServices.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.PathRegexTimeout;
            Regex path_matcher=new Regex(Filter, IgnoreCase | Compiled | CultureInvariant, TimeOut);
            Func<HttpContext, Boolean> filter = context => path_matcher.IsMatch(context.Request.Path); 
            return UseActiveSessions(Builder, new SimplePredicateFilterSource(filter, "Path|\""+Filter+"\""));
        }

        /// <summary>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/summary' />
        /// </summary>
        /// <param name="Builder">
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/param[@name="Builder"]' />
        /// </param>
        /// <param name="Filter">
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, Func{HttpContext,Boolean})" path='/param[@name="Filter"]' />
        /// </param>
        /// <param name="Suffix">The suffix to be added to an <see cref="IActiveSession.Id">Id</see> property 
        /// of active sessions assigned to requests that falls into the scope of the filter set by this method.</param>
        /// <returns> <inheritdoc cref = "UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/returns' /> </returns>
        /// <remarks>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/remarks/toinherit' />
        /// <para>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, Func{HttpContext,Boolean})" path='/remarks/para/filter'/>
        /// <suffix>
        /// and passes the value of the <paramref name="Suffix"/> parameter as a suffix to be added to an active session identifier.
        /// </suffix>
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder, Func<HttpContext, Boolean> Filter, String Suffix)
        {
            return Builder.UseActiveSessions(new PredicateWithSuffixFilterSource( Filter,Suffix));
        }


        /// <summary>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/summary' />
        /// </summary>
        /// <param name="Builder">
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/param[@name="Builder"]' />
        /// </param>
        /// <param name="Filter">
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, String, TimeSpan)" path='/param[@name="Filter"]' />
        /// </param>
        /// <param name="Suffix">
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, Func{HttpContext, bool}, string)" path='/param[@name="Suffix"]'/>
        /// </param>
        /// <param name="TimeOut">
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, String, TimeSpan)" path='/param[@name="TimeOut"]' />
        /// </param>
        /// <returns> <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/returns' /> </returns>
        /// <remarks>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, IMiddlewareFilterSource?)" path='/remarks/toinherit' />
        /// <para>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, Func{HttpContext,Boolean})" path='/remarks/para/filter'/>
        /// <inheritdoc cref="UseActiveSessions(IApplicationBuilder, Func{HttpContext, bool}, string)" path='/remarks/para/suffix'/>
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseActiveSessions(this IApplicationBuilder Builder, String Filter, String Suffix, TimeSpan TimeOut = default)
        {
            if(TimeOut==default)
                TimeOut=Builder.ApplicationServices.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.PathRegexTimeout;
            Regex path_matcher = new Regex(Filter, IgnoreCase | Compiled | CultureInvariant, TimeOut);
            Func<HttpContext, Boolean> filter = context => path_matcher.IsMatch(context.Request.Path);
            return UseActiveSessions(Builder, new PredicateWithSuffixFilterSource(filter, Suffix, "Path|\""+Filter+"\""));
        }

        internal const String ACTIVESESSION_PROPERTYNAME = "__ActiveSession__";

    }
}
