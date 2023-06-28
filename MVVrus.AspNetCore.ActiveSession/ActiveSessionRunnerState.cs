namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Enumeration containing possible states of a runner object? implementing a <see cref="IActiveSessionRunner{TResult}"/>interface.
    /// </summary>
    public enum ActiveSessionRunnerState
    {
        /// <value> Just created and not started </value>
        NotStarted=0,
        /// <value> Started but stays at the same position  and has no new data available</value>
        Stalled=1,
        /// <value> Advanced to a new position or completed and has new data available</value>
        Progressed=2,
        /// <value> Completed and has no new data available</value>
        Complete=3,
        /// <value> An error occured while the object is running, no data available</value>
        Failed=-1,
        /// <value> An error occured while the object is running, no data available</value>
        Aborted=-2,
    }
}
