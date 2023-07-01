namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The structure used to return result of execution some runner interface methots
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned in the <paramref name="Result"/> </typeparam>
    /// <param name="Result">The returned value</param>
    /// <param name="State">The runner state in the moment of return</param>
    /// <param name="Position">The runner position in the moment of return</param>
    /// <param name="FailureException">Reason of the <see cref="ActiveSessionRunnerState.Failed"/> state</param>
    /// <remarks> TODO which methods?</remarks>
    public record struct ActiveSessionRunnerResult<TResult>(
        TResult Result, 
        ActiveSessionRunnerState State, 
        Int32 Position,
        Exception? FailureException=null
     ); 
}
