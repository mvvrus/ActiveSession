namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Extension of <see cref="IActiveSessionRunner{TResult}"/> to support remote runner calls. Not implemented yet
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IActiveSessionRunnerProxy<TResult>:IActiveSessionRunner<TResult>
    {
        /// <value>
        /// Does the runner execute in the same instance?
        /// </value>
        public Boolean IsLocal { get; }

        /// <summary>
        /// Asynchronous <see cref="IActiveSessionRunner.State"/> property counterpart
        /// </summary>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a value of the <see cref="IActiveSessionRunner.State"/> property (possibly from a remote instance) </returns>
        public ValueTask<ActiveSessionRunnerState> GetStateAsync();

        /// <summary>
        /// Asynchronous <see cref="IActiveSessionRunner.Position"/> property counterpart
        /// </summary>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a value of the <see cref="IActiveSessionRunner.Position"/> property (possibly from a remote instance) </returns>
        public ValueTask<Int32> GetPositionAsync();

        /// <summary>
        /// Asynchronous <see cref="IActiveSessionRunner{TResult}.GetAvailable(int)"/> method counterpart
        /// </summary>
        /// <returns> 
        /// A <see cref="ValueTask{T}"/> wrapping a value returned by 
        /// <see cref="IActiveSessionRunner{TResult}.GetAvailable(int)"/> method call (possibly from a remote instance) 
        /// </returns>
        public ValueTask<ActiveSessionRunnerResult<TResult>> GetAvailableAsync(Int32 StartPosition);
    }
}
