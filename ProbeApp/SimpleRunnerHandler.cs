using MVVrus.AspNetCore.ActiveSession;

namespace ProbeApp
{
    public static class SimpleRunnerHandler
    {
        public static String PageHandler(HttpContext context)
        {
            IActiveSession? active_session = context.Features.Get<IActiveSessionFeature>()?.ActiveSession;
            SimpleRunnerParams runner_params= new SimpleRunnerParams {Immediate=10, End=100};
            (IActiveSessionRunner<int> runner, int runner_key) = active_session?.CreateRunner<SimpleRunnerParams,int>(runner_params, context.TraceIdentifier)??default;
            

            return "Simple ActiveSession runner handler. ActiveSession.Id="+active_session?.Id??"<null>";
        }
    }
}
