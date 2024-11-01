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
        readonly MiddlewareMapper _mapper;

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
            _logger=LoggerFactory?.CreateLogger(MIDDLEWARE_CATEGORY_NAME);
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
            _mapper=new MiddlewareMapper(FilterParam);
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
                (Boolean pass, String? suffix)=_mapper.MapContext(Context);
                if (pass) {
                    feature=_store.AcquireFeatureObject(Context.Session, Context.TraceIdentifier, suffix);
                    //TODO test passing suffix to AcquireFeatureObject
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
            public List<IMiddlewareFilterSource> Filters = new List<IMiddlewareFilterSource>();
        }

        internal record struct MapContextResult(Boolean WasMapped, String? SessionSuffix);

        internal class MiddlewareMapper
        {
            Boolean _canSetSuffix = false;
            Boolean _acceptAll;
            List<IMiddlewareFilter> _filters = new List<IMiddlewareFilter>();

            public MiddlewareMapper(MiddlewareParam Source)
            {
                _acceptAll=Source.AcceptAll;
                for(int order=0; order<Source.Filters.Count;order++) AddFilterSource(Source.Filters[order], order);
            }

            public MapContextResult MapContext(HttpContext Context)
            {
                Boolean dont_scan_all = _acceptAll && !_canSetSuffix;

                Boolean was_mapped = false;
                String? session_suffix = null;
                int order = Int32.MaxValue;

                for(int i = 0; i<_filters.Count && (
                        (session_suffix == null && _canSetSuffix) || 
                        (!_acceptAll && (!was_mapped || order>_filters[i].MinOrder)) //TODO(future) Middleware filter grouping: test this
                    ); i++) 
                {
                    (Boolean mapped_here, String? mapped_suffix, Int32 mapped_order) = _filters[i].Apply(Context);
                    was_mapped = was_mapped || mapped_here;
                    if(mapped_order<order) {
                        //TODO(future) Middleware filter grouping: test this
                        order=mapped_order;
                        session_suffix=mapped_suffix??session_suffix;
                    }
                }
                was_mapped = was_mapped || _acceptAll;
                return new MapContextResult(was_mapped, session_suffix);
            }

            void AddFilterSource(IMiddlewareFilterSource FilterSource, Int32 Order)
            {
                _canSetSuffix = _canSetSuffix || FilterSource.HasSuffix;
                Boolean added = false;
                if(FilterSource.IsGroupable) {
                    throw new NotImplementedException(); //TODO(future) Middleware filter grouping
                }
                if(!added) _filters.Add(FilterSource.Create(Order));
            }

        }

    }
}
