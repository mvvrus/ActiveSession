namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Enumeration containing possible states of a runner object? implementing a <see cref="IActiveSessionRunner{TResult}"/>interface.
    /// </summary>
    public enum ActiveSessionRunnerState
    {
        /// <value> Just created and not started </value>
        NotStarted,
        /// <value> Stays at the same position  and has no new data available</value>
        Stalled,
        /// <value> Advanced to a new position or completed and has new data available</value>
        Progressed,
        /// <value> Completed and has no new data available</value>
        Complete,
        /// <value> An error occured while the object is running, no data available</value>
        Failed,
        /// <value> An error occured while the object is running, no data available</value>
        Aborted,
    }
}
