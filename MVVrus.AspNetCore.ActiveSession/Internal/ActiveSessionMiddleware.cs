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
        readonly Boolean _preloadActiveSession;
        readonly List<Func<HttpContext,Boolean>> _filters;
        readonly Boolean _acceptAll;

        //Properties for testing
        internal RequestDelegate Next { get { return _next; } }
        internal IActiveSessionStore Store { get { return _store; } }

        public ActiveSessionMiddleware(RequestDelegate Next,
            MiddlewareParam FilterParam,
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
                _preloadActiveSession=Options.Value.PreloadActiveSession;
            }
            catch (Exception exception) {
                _logger?.LogErrorMiddlewareCannotBeCreated(exception);
                #if TRACE
                _logger?.LogTraceConstructActiveSessionMiddlewareExit();
                #endif
                throw;
            }
            _acceptAll=FilterParam.AcceptAll;
            _filters=FilterParam.Filters;
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
            IActiveSessionFeature? feature = null;
            try {
                Boolean pass = _acceptAll;
                for (int i = 0; !pass&&i<_filters.Count; i++)
                    pass=pass||_filters[i].Invoke(Context);
                if (pass) {
                    feature=_store.AcquireFeatureObject(Context.Session, Context.TraceIdentifier);
                    Context.Features.Set(feature);
                    _logger?.LogDebugActiveSessionFeatureActivated(Context.TraceIdentifier);
                    if (_preloadActiveSession||_useSessionServicesAsRequestServices) {
                        #if TRACE
                        _logger?.LogTraceWaitingForActiveSessionLoading(Context.TraceIdentifier);
                        #endif
                        await feature.LoadAsync();
                    }
                    if (_useSessionServicesAsRequestServices) {
                        if (feature.IsLoaded&&feature.ActiveSession.IsAvailable) {
                            Context.RequestServices=feature.ActiveSession.SessionServices;
                            _logger?.LogDebugRequestServicesChangedToSessionServices(Context.TraceIdentifier);
                        }
                        #if TRACE
                        _logger?.LogTraceCompleteRequestServicesSubstitutionAttempt(Context.TraceIdentifier);
                        #endif
                    }

                }
                else Context.Features.Set((IActiveSessionFeature?)null);
                #if TRACE
                _logger?.LogTraceActiveSessionMiddlewareInvokeRest(Context.TraceIdentifier);
                #endif
                await _next(Context);
                #if TRACE
                _logger?.LogTraceActiveSessionMiddlewareControlReturns(Context.TraceIdentifier);
                #endif
                if(feature!=null) await feature!.CommitAsync();
            }
            catch (Exception exception) {
                #if TRACE
                _logger?.LogTracePipelineException(exception, Context.TraceIdentifier);
                #endif
                throw;
            }
            finally {
                Context.Features.Set((IActiveSessionFeature?)null);
                if(feature!=null) _store.ReleaseFeatureObject(feature);
                Context.RequestServices=request_services;
                #if TRACE
                _logger?.LogTraceActiveSessionMiddlewareExit(Context.TraceIdentifier);
                #endif
            }
        }

        internal class MiddlewareParam
        {
            public Boolean AcceptAll;
            public List<Func<HttpContext, Boolean>> Filters = new List<Func<HttpContext, Boolean>>();

        }
    }
}
