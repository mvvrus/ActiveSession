namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO
    /// </summary>
    public static class ActiveSessionExtensions
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Session"></param>
        /// <param name="Request"></param>
        /// <param name="Context"></param>
        /// <param name="ExclusiveService"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static KeyedRunner<TResult> CreateExclusiveRunner<TRequest,TResult> (
            this IActiveSession Session,
            TRequest Request,
            HttpContext Context,
            IDisposable ExclusiveService)
        {
            KeyedRunner<TResult> result = Session.CreateRunner<TRequest, TResult>(Request, Context);
            (Session.TrackRunnerCleanup(result.RunnerNumber)??Task.CompletedTask).ContinueWith((_)=>ExclusiveService.Dispose());
            return result;
        }
    }
}
