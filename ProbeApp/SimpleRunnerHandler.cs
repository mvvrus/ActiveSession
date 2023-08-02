using MVVrus.AspNetCore.ActiveSession;

namespace ProbeApp
{
    public static class SimpleRunnerHandler
    {
        public static async Task<string> PageHandler(HttpContext context, int? RunnerNumber)
        {
            IActiveSession? active_session = context.Features.Get<IActiveSessionFeature>()?.ActiveSession;
            SimpleRunnerParams runner_params= new SimpleRunnerParams {Immediate=10, End=100};
            String id = active_session?.Id??"<null>";
            (IActiveSessionRunner<int> runner, int runner_key) = active_session?.CreateRunner<SimpleRunnerParams,int>(runner_params, context.TraceIdentifier)??default;
            ActiveSessionRunnerResult<int> runner_result = await runner.GetMoreAsync(-1);
            String result = $"Simple ActiveSession runner handler. ActiveSession.Id={id}, Runner key ={runner_key}, Runner result={runner_result}";
            return result;
        }
    }
}
