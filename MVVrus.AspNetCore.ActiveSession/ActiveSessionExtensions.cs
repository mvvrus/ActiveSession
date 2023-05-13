namespace MVVrus.AspNetCore.ActiveSession
{
    public static class ActiveSessionExtensions
    {
        public static IActiveSessionRunner<TResult> GetNewRunner<TRequest, TResult>(
            this IActiveSession ActiveSession, 
            TRequest Request,
            out Int32 Key
            ) 
        {
            KeyedActiveSessionRunner<TResult> runner_and_key;
            runner_and_key= ActiveSession.GetRunner<TRequest,TResult> (Request);
            Key=runner_and_key.Key;
            return runner_and_key.Runner;
        }

        public static IActiveSessionRunner<TResult>? GetExistingRunner<TResult> (
            this IActiveSession ActiveSession,
            Int32 Key
            )
        {
            return ActiveSession.GetRunner<TResult>(Key);
        }

        public static IActiveSessionRunner<TResult> GetAnyRunner<TRequest, TResult>(
            this IActiveSession ActiveSession,
            TRequest Request,
            ref Int32 Key
            )
        {
            return ActiveSession.GetRunner<TResult>(Key) ?? 
                ActiveSession.GetNewRunner<TRequest, TResult>(Request, out Key);
        }

    }
}
