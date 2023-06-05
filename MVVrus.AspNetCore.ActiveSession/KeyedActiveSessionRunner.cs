namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The type used to return newly created runner
    /// </summary>
    /// <typeparam name="TResult">Type specializing the runner's <see cref="IActiveSessionRunner{TResult}"/> interface</typeparam>
    /// <param name="Runner">The runner object returned</param>
    /// <param name="Key">The integer key to be used to access the runner object  later via <see cref="IActiveSession.GetRunner{TResult}(int, HttpContext?)"></see> method</param>
    public record struct KeyedActiveSessionRunner<TResult>
    (
        IActiveSessionRunner<TResult> Runner,
        Int32 Key
    );
}
