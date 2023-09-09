using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionMiddleware
    {
        readonly RequestDelegate _next;
        readonly IActiveSessionStore _store;
        readonly ILogger? _logger;
        readonly Boolean _useSessionServicesAsRequestServices;

        //Properties for testing
        internal RequestDelegate Next { get { return _next; } }
        internal IActiveSessionStore Store { get { return _store; } }
        internal Boolean useSessionServicesAsRequestServices { get { return _useSessionServicesAsRequestServices; } }

        public ActiveSessionMiddleware(RequestDelegate Next,
            IActiveSessionStore Store,
            ILoggerFactory? LoggerFactory,
            IOptions<ActiveSessionOptions> Options
        )
        {
            _logger=LoggerFactory?.CreateLogger(LOGGING_CATEGORY_NAME);
            #if TRACE
            _logger?.LogTraceConstructActiveSessionMiddleware();
            #endif
            try {
                _next=Next??throw new ArgumentNullException(nameof(Next));
                _store=Store??throw new ArgumentNullException(nameof(Store));
                _logger?.LogInformationActiveSessionMiddlewareAdded();
                _useSessionServicesAsRequestServices=Options.Value.UseSessionServicesAsRequestServices;
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
            IServiceProvider request_services = Context.RequestServices;
            IActiveSessionFeature feature = 
                _store.CreateFeatureObject(Context.Session, Context.TraceIdentifier);
            Context.Features.Set(feature);
            _logger?.LogDebugActiveSessionFeatureActivated(Context.TraceIdentifier);
            try {
                if(_useSessionServicesAsRequestServices) {
                    #if TRACE
                    _logger?.LogTraceWaitingForActiveSessionLoading(Context.TraceIdentifier);
                    #endif
                    await feature.LoadAsync();
                    if(feature.IsLoaded && Context.Session.IsAvailable && feature.ActiveSession.IsAvailable) {
                        Context.RequestServices=feature.ActiveSession.SessionServices;
                        _logger?.LogDebugRequestServicesChangedToSessionServices(Context.TraceIdentifier);
                    }
                    #if TRACE
                    _logger?.LogTraceCompleteRequestServicesSubstitutionAttempt(Context.TraceIdentifier);
                    #endif
                }
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
                #if TRACE
                _logger?.LogTracePipelineException(exception, Context.TraceIdentifier);
                #endif
                throw;
            }
            finally {
                Context.Features.Set<IActiveSessionFeature>(null);
                feature?.Clear();
                Context.RequestServices=request_services;
                #if TRACE
                _logger?.LogTraceActiveSessionMiddlewareExit(Context.TraceIdentifier);
                #endif
            }
        }
    }
}
