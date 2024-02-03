namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Extension of <see cref="IRunner{TResult}"/> to support remote runner calls. Not implemented yet
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IRunnerProxy<TResult>:IRunner<TResult>
    {
        /// <value>
        /// Does the runner execute in the same instance?
        /// </value>
        public Boolean IsLocal { get; }

        /// <summary>
        /// Asynchronous <see cref="IRunner.State"/> property counterpart
        /// </summary>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a value of the <see cref="IRunner.State"/> property (possibly from a remote instance) </returns>
        public ValueTask<RunnerStatus> GetStateAsync();

        /// <summary>
        /// Asynchronous <see cref="IRunner.Position"/> property counterpart
        /// </summary>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a value of the <see cref="IRunner.Position"/> property (possibly from a remote instance) </returns>
        public ValueTask<Int32> GetPositionAsync();

        /// <summary>
        /// Asynchronous <see cref="IRunner{TResult}.GetAvailable(int, int, String?)"/> method counterpart
        /// </summary>
        /// <returns> 
        /// A <see cref="ValueTask{T}"/> wrapping a value returned by 
        /// <see cref="IRunner{TResult}.GetAvailable(int, int, String?)"/> method call (possibly from a remote instance) 
        /// </returns>
        public ValueTask<RunnerResult<TResult>> GetAvailableAsync(Int32 StartPosition);
    }
}
