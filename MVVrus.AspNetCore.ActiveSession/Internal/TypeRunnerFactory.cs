namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //TODO Add logging
    internal class TypeRunnerFactory<TRequest, TResult> :
        IActiveSessionRunnerFactory<TRequest, TResult>
    {
        readonly object[] _params;
        readonly Type _runner_type;

        public TypeRunnerFactory(Type RunnerType, object[]? Params)
        {
            _runner_type = RunnerType;
            int params_length = Params?.Length ?? 0;
            _params = new object[params_length + 1];
            if (params_length > 0) Array.Copy(Params!, 0, _params, 1, params_length);
        }

        public IActiveSessionRunner<TResult>? Create(TRequest Request, IServiceProvider Services)
        {
            //Put Request as a first parameter
            _params[0] = Request!;
            //TODO Add logging
            //TODO Change reaction on null
            return ActivatorUtilities.CreateInstance(Services, _runner_type, _params) as IActiveSessionRunner<TResult>;
            throw new NotImplementedException();
        }
    }
}
