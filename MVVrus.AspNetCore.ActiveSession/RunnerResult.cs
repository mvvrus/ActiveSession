namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Used to return result of a runner execution. 
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned in the <paramref name="Result"/> </typeparam>
    /// <param name="Result">The returned value</param>
    /// <param name="Status">The runner status in the moment of return</param>
    /// <param name="Position">The runner position in the moment of return</param>
    /// <param name="FailureException">Reason of the <see cref="RunnerStatus.Failed"/> status</param>
    /// <remarks>
    /// The result mentioned is returned via <see cref="IRunner{TResult}.GetAvailable(int, int, string?)">GetAvailable</see>
    /// or <see cref="IRunner{TResult}.GetRequiredAsync(int, CancellationToken, int, string?)">GetAvailable</see> metods.
    /// </remarks>
    public record struct RunnerResult<TResult>(
        TResult Result,
        RunnerStatus Status,
        Int32 Position,
        Exception? FailureException = null
     )
    {
        /// <summary>
        /// Convert a tuple of values with appropriate types to an instance of this struct.
        /// </summary>
        /// <param name="Value">The value to be converted.</param>
        public static  implicit operator RunnerResult<TResult>(ValueTuple<TResult,RunnerStatus,Int32> Value)
        {
            return new RunnerResult<TResult>(Value.Item1, Value.Item2, Value.Item3);
        }
    }

}
