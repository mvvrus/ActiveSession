namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The structure used to return result of execution some runner interface methots
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned in the <paramref name="Result"/> </typeparam>
    /// <param name="Result">The returned value</param>
    /// <param name="Status">The runner status in the moment of return</param>
    /// <param name="Position">The runner position in the moment of return</param>
    /// <param name="FailureException">Reason of the <see cref="RunnerStatus.Failed"/> status</param>
    /// <remarks> TODO which methods?</remarks>
    public record struct RunnerResult<TResult>(
        TResult Result, 
        RunnerStatus Status, 
        Int32 Position,
        Exception? FailureException=null
     ); 
}
