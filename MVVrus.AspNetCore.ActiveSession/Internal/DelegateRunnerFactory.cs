using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class DelegateRunnerFactory<TRequest, TResult> : IActiveSessionRunnerFactory<TRequest, TResult>
    {
        readonly Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> _factory;
        readonly ILogger? _logger;

        public DelegateRunnerFactory(Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> Factory, ILogger? Logger=null)
        {
            _logger = Logger;
            #if TRACE
            _logger?.LogTraceConstructDelegateFactory(typeof(TRequest).FullName??UNKNOWN_TYPE, typeof(TResult).FullName??UNKNOWN_TYPE);
            #endif
            _factory= Factory;
        }

        public IActiveSessionRunner<TResult> Create(TRequest Request, IServiceProvider Services)
        {
            #if TRACE
            _logger?.LogTraceInvokingDelegateFactory(typeof(TRequest).FullName??UNKNOWN_TYPE, typeof(TResult).FullName??UNKNOWN_TYPE);
            #endif
            return _factory(Request, Services);
        }
    }
}
