namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Enumeration containing possible states of a runner object? implementing a <see cref="IRunner{TResult}"/>interface.
    /// </summary>
    public enum RunnerState
    {
        /// <value> Just created and not started </value>
        NotStarted=0,
        /// <value> Started but stays at the same position  and has no new data available</value>
        Stalled=1,
        /// <value> Advanced to a new position or completed and has new data available</value>
        Progressed=2,
        /// <value> Completed and has no new data available</value>
        Complete=10,
        /// <value> An error occured while the object is running, no data available</value>
        Failed=11,
        /// <value> An error occured while the object is running, no data available</value>
        Aborted=12,
    }

    /// <summary>
    /// Class containing an extension method for <see cref="RunnerState"/> enumerable
    /// </summary>
    public static class RunnerStateExtensions
    {
        /// <summary>
        /// Returns true if the state, specified by <paramref name="State"/> is final, 
        /// i.e. that a runner execution beyond that state will never proceed.
        /// </summary>
        /// <param name="State">The runner state to analyze/</param>
        /// <returns>true if the <paramref name="State"/> specified is a final one (Complete, Failed or Aborted), false otherwise</returns>
        public static Boolean IsFinal(this RunnerState State)
        {
            return State>=RunnerState.Complete;
        }

    }
}
