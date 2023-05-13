namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class DelegateRunnerFactory<TRequest, TResult> : IActiveSessionRunnerFactory<TRequest, TResult>
    {
        readonly Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> _factory;

        public DelegateRunnerFactory(Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> Factory)
        {
            _factory = Factory;
        }

        public IActiveSessionRunner<TResult> Create(TRequest Request, IServiceProvider Services)
        {
            return _factory(Request, Services);
        }
    }
}
