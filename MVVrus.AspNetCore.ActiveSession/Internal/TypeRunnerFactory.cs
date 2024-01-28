using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //TODO Add logging
    internal class TypeRunnerFactory<TRequest, TResult> :
        IRunnerFactory<TRequest, TResult>
    {
        const Int32 MIN_REQ_PARAMS = 1;
        const Int32 MAX_REQ_PARAMS = 2;
        readonly object[] _params;
        readonly Type _runner_type;
        readonly ILogger? _logger;
        readonly Int32 _reqParamsCount;

        public TypeRunnerFactory(Type RunnerType, object[]? Params, Int32 ReqParamsCount, ILoggerFactory? LoggerFactory)
        {
            _logger=LoggerFactory?.CreateLogger(LOGGING_CATEGORY_NAME);
            #if TRACE
            _logger?.LogTraceConstructTypeFactory(
                typeof(TRequest).FullName??UNKNOWN_TYPE, 
                typeof(TResult).FullName??UNKNOWN_TYPE,
                GetType().FullName!
            );
            #endif
            _reqParamsCount=ReqParamsCount;
            if (_reqParamsCount<MIN_REQ_PARAMS||_reqParamsCount>MAX_REQ_PARAMS)
                throw new ArgumentOutOfRangeException($"TypeRunerFactory: invalid number of required runner constructor parameters ({_reqParamsCount})");
            _runner_type= RunnerType;
            int params_length = Params?.Length ?? 0;
            _params = new object[params_length + _reqParamsCount];
            if (params_length > 0) Array.Copy(Params!, 0, _params, _reqParamsCount, params_length);
        }

        public IRunner<TResult>? Create(TRequest Request, IServiceProvider Services, RunnerId RunnerId, String? TraceIdentifier = null)
        {
            //Put Request as a first parameter
            #if TRACE
            _logger?.LogTraceInvokingTypeFactory(
                typeof(TRequest).FullName??UNKNOWN_TYPE, 
                typeof(TResult).FullName??UNKNOWN_TYPE, 
                RunnerId,
                TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER);
            #endif
            _params[0] = Request!;
            if(_reqParamsCount>1) _params[1] = RunnerId;

            return ActivatorUtilities.CreateInstance(Services, _runner_type, _params) as IRunner<TResult>;
        }
    }
}
