using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class DelegateRunnerFactory<TRequest, TResult> : IRunnerFactory<TRequest, TResult>
    {
        readonly Func<TRequest, IServiceProvider, IRunner<TResult>> _factory;
        readonly ILogger? _logger;

        //For testing purposes
        internal Func<TRequest, IServiceProvider, IRunner<TResult>> Factory { get { return _factory; } }

        public DelegateRunnerFactory(Func<TRequest, IServiceProvider, IRunner<TResult>> Factory, ILogger? Logger=null)
        {
            _logger = Logger;
            #if TRACE
            _logger?.LogTraceConstructDelegateFactory(typeof(TRequest).FullName??UNKNOWN_TYPE, typeof(TResult).FullName??UNKNOWN_TYPE);
            #endif
            _factory= Factory;
        }

        public IRunner<TResult> Create(TRequest Request, IServiceProvider Services)
        {
            #if TRACE
            _logger?.LogTraceInvokingDelegateFactory(typeof(TRequest).FullName??UNKNOWN_TYPE, typeof(TResult).FullName??UNKNOWN_TYPE);
            #endif
            return _factory(Request, Services);
        }
    }
}
