namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The type of structure used to return newly created runner
    /// </summary>
    /// <typeparam name="TResult">Type specializing the runner's <see cref="IRunner{TResult}"/> interface</typeparam>
    /// <param name="Runner">A reference to the runner object returned</param>
    /// <param name="RunnerNumber">
    /// The number (integer key) for the runner object that is unique within the Active Session. 
    /// It is intended to be used to access the runner object later via 
    /// <see cref="IActiveSession.GetRunner{TResult}(int, HttpContext)"></see> method
    /// </param>
    public record struct KeyedRunner<TResult>
    (
        IRunner<TResult> Runner,
        Int32 RunnerNumber
    );
}
