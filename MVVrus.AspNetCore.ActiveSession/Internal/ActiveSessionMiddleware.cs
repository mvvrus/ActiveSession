using Microsoft.AspNetCore.Http.Features;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionMiddleware
    {
        readonly RequestDelegate _next;
        readonly IActiveSessionStore _store;
        readonly ILogger? _logger;

        public ActiveSessionMiddleware(RequestDelegate Next, IActiveSessionStore Store, ILoggerFactory? LoggerFactory)
        {
            _logger=LoggerFactory?.CreateLogger(LOGGING_CATEGORY_NAME);
            #if TRACE
            _logger?.LogTraceConstructActiveSessionMiddleware();
            #endif
            try {
                _next=Next??throw new ArgumentNullException(nameof(Next));
                _store=Store??throw new ArgumentNullException(nameof(Store));
                _logger?.LogInformationActiveSessionMiddlewareAdded();
            }
            catch (Exception exception) {
                _logger?.LogErrorMiddlewareCannotBeCreated(exception);
                #if TRACE
                _logger?.LogTraceConstructActiveSessionMiddlewareExit();
                #endif
                throw;
            }
            #if TRACE
            _logger?.LogTraceConstructActiveSessionMiddlewareExit();
            #endif
        }

        public async Task Invoke(HttpContext Context)
        {
            #if TRACE
            _logger?.LogTraceInvokeActiveSessionMiddleware(Context.TraceIdentifier);
            #endif
            IActiveSessionFeature feature = new ActiveSessionFeature(
                _store, Context.Features.Get<ISessionFeature>()?.Session, _logger, Context.TraceIdentifier);
            Context.Features.Set(feature);
            _logger?.LogDebugActiveSessionFeatureActivated(Context.TraceIdentifier);
            try {
                #if TRACE
                _logger?.LogTraceActiveSessionMiddlewareInvokeRest(Context.TraceIdentifier);
                #endif
                await _next(Context);
                #if TRACE
                _logger?.LogTraceActiveSessionMiddlewareControlReturns(Context.TraceIdentifier);
                #endif
                await feature.CommitAsync();
            }
            catch (Exception exception) {
                _logger?.LogTracePipelineException(exception, Context.TraceIdentifier);
                throw;
            }
            finally {
                Context.Features.Set<IActiveSessionFeature>(null);
                feature.Clear();
                #if TRACE
                _logger?.LogTraceActiveSessionMiddlewareExit(Context.TraceIdentifier);
                #endif
            }
        }
    }
}
