using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //TODO Add logging
    internal class TypeRunnerFactory<TRequest, TResult> :
        IActiveSessionRunnerFactory<TRequest, TResult>
    {
        readonly object[] _params;
        readonly Type _runner_type;
        readonly ILogger? _logger;

        public TypeRunnerFactory(Type RunnerType, object[]? Params, ILoggerFactory? LoggerFactory)
        {
            _logger=LoggerFactory?.CreateLogger(LOGGING_CATEGORY_NAME);
            #if TRACE
            _logger?.LogTraceConstructTypeFactory(
                typeof(TRequest).FullName??UNKNOWN_TYPE, 
                typeof(TResult).FullName??UNKNOWN_TYPE,
                this.GetType().FullName!
            );
            #endif
            _runner_type= RunnerType;
            int params_length = Params?.Length ?? 0;
            _params = new object[params_length + 1];
            if (params_length > 0) Array.Copy(Params!, 0, _params, 1, params_length);
        }

        public IActiveSessionRunner<TResult>? Create(TRequest Request, IServiceProvider Services)
        {
            //Put Request as a first parameter
            #if TRACE
            _logger?.LogTraceInvokingTypeFactory(typeof(TRequest).FullName??UNKNOWN_TYPE, typeof(TResult).FullName??UNKNOWN_TYPE);
            #endif
            _params[0] = Request!;
            return ActivatorUtilities.CreateInstance(Services, _runner_type, _params) as IActiveSessionRunner<TResult>;
            throw new NotImplementedException();
        }
    }
}
