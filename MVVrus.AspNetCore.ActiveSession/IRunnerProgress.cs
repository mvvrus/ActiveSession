namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO
    /// </summary>
    public interface IRunnerProgress
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        (Int32 Progress, Int32? EstimatedEnd) GetProgress();
    }
}
