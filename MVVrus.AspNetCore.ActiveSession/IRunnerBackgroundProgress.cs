namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Allows obtaining information about progress of a background operation of a runner
    /// </summary>
    public interface IRunnerBackgroundProgress
    {
        /// <summary>
        /// Returns information about progress of background operation of a runner.
        /// </summary>
        /// <returns> An unnamed record containing a pair of values: 
        ///   <para>Progress: a <see cref="IRunner.Position"/> value wich a background execution have been reached</para>
        ///   <para>EstimatedEnd: estimation of a <see cref="IRunner.Position"/> value to be at a finish of the background execution(if any)</para>
        /// </returns>
        (Int32 Progress, Int32? EstimatedEnd) GetProgress();
        /// <summary>
        /// Indicate whether the background operation is completed.
        /// </summary>
        Boolean IsBackgroundExecutionCompleted { get; }
    }
}
