using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class DelegateRunnerFactory<TRequest, TResult> : IRunnerFactory<TRequest, TResult>
    {
        readonly Func<TRequest, IServiceProvider, RunnerId, IRunner<TResult>> _factory;
        readonly ILogger? _logger;

        public DelegateRunnerFactory(Func<TRequest, IServiceProvider, RunnerId, IRunner<TResult>> Factory, ILoggerFactory? LoggerFactory=null)
        {
            _logger = LoggerFactory?.CreateLogger(FACTORY_CATEGORY_NAME);
            #if TRACE
            _logger?.LogTraceConstructDelegateFactory(typeof(TRequest).FullName??UNKNOWN_TYPE, typeof(TResult).FullName??UNKNOWN_TYPE);
            #endif
            _factory= Factory;
        }

        public IRunner<TResult> Create(TRequest Request, IServiceProvider Services, RunnerId RunnerId, String? TraceIdentifier = null)
        {
            #if TRACE
            _logger?.LogTraceInvokingDelegateFactory(
                typeof(TRequest).FullName??UNKNOWN_TYPE, 
                typeof(TResult).FullName??UNKNOWN_TYPE, 
                RunnerId,
                TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER);
            #endif
            return _factory(Request, Services, RunnerId);
        }
    }
}
