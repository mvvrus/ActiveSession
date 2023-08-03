using MVVrus.AspNetCore.ActiveSession;

namespace ProbeApp
{
    public static class SimpleRunnerHandler
    {
        public static async Task<string> PageHandler(HttpContext Context, int? RunnerNumber)
        {
            IActiveSession? active_session = Context.Features.Get<IActiveSessionFeature>()?.ActiveSession;
            SimpleRunnerParams runner_params= new SimpleRunnerParams {Immediate=10, End=100};
            String id = active_session?.Id??"<null>";
            int runner_key=-1;
            IActiveSessionRunner<int>? runner=null;
            if (RunnerNumber.HasValue) {
                runner_key=RunnerNumber.Value;
                runner=active_session?.GetRunner<int>(runner_key, Context);
            }
            if(runner==null)
                (runner, runner_key) = active_session?.CreateRunner<SimpleRunnerParams,int>(runner_params, Context)??default;
            ActiveSessionRunnerResult<int> runner_result = await runner.GetMoreAsync(-1);
            String result;
            String head = "";
            String body = "";

            body+=$"Simple ActiveSession runner handler. ActiveSession.Id={id}, Runner key={runner_key}" +
                $"<BR/> Runner result={runner_result}";
            body+=$"<BR/> <A href=\"/SimpleRunner?RunnerNumber={runner_key}\">Proceed with this runner</A>";
            Context.Response.ContentType="text/html";
            result="<!DOCTYPE HTML><HTML><HEAD>"+head+"</HEAD><BODY>"+body+"</BODY></HTML>";
            return result;
        }
    }
}
