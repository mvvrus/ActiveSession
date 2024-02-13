namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// TODO
    /// </summary>
    public interface IRunnerProgressSetter<TResult>
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Result"></param>
        /// <param name="EstimatedEnd"></param>
        void SetProgress(TResult Result, Int32? EstimatedEnd);
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Result"></param>
        void SetResult(TResult Result);
    }
}
