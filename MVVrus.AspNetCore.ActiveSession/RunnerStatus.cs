namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Enumeration containing possible states of a runner object? implementing a <see cref="IRunner{TResult}"/>interface.
    /// </summary>
    public enum RunnerStatus
    {
        /// <summary> Just created and not started </summary>
        NotStarted=0,
        /// <summary> Started but stays at the same position  and has no new data available</summary>
        Stalled=1,
        /// <summary> Advanced to a new position or completed and has new data available</summary>
        Progressed=2,
        /// <summary> Completed and has no new data available</summary>
        Complete=10,
        /// <summary> An error occured while the object is running, no data available</summary>
        Failed=11,
        /// <summary> An error occured while the object is running, no data available</summary>
        Aborted=12,
    }

    /// <summary>
    /// Class containing an extension method for <see cref="RunnerStatus"/> enumerable
    /// </summary>
    public static class RunnerStatusExtensions
    {
        /// <summary>
        /// Returns true if the state, specified by <paramref name="State"/> is final, 
        /// i.e. that a runner execution beyond that state will never proceed.
        /// </summary>
        /// <param name="State">The runner state to analyze/</param>
        /// <returns>true if the <paramref name="State"/> specified is a final one (Complete, Failed or Aborted), false otherwise</returns>
        public static Boolean IsFinal(this RunnerStatus State)
        {
            return State>=RunnerStatus.Complete;
        }

        /// <summary>
        /// Returns true if the state, specified by <paramref name="State"/> is final, 
        /// i.e. that a runner execution beyond that state will never proceed.
        /// </summary>
        /// <param name="State">The runner state to analyze/</param>
        /// <returns>true if the <paramref name="State"/> specified is a final one (Complete, Failed or Aborted), false otherwise</returns>
        public static Boolean IsRunning(this RunnerStatus State)
        {
            return State == RunnerStatus.Stalled || State == RunnerStatus.Progressed;
        }

    }
}
