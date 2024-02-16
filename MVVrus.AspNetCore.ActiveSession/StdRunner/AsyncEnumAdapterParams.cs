namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Class containg parametrs to pass to a <see cref="AsyncEnumAdapterRunner{TResult}"/> constructor
    /// </summary>
    /// <typeparam name="TResult">Type specializing the runner's <see cref="IRunner{TResult}"/> interface</typeparam>
    /// <param name="Source">The base object implementing <see cref="IAsyncEnumerable{T}"/> for which the adapter to be created</param>
    /// <param name="DefaultAdvance">
    /// Default maximum number of elements acquired from the <paramref name="Source"/> 
    /// to be returned to the caller in one call(currently defaults to 20)
    /// </param>
    /// <param name="CompletionTokenSource">
    /// An external <see cref="CancellationTokenSource"/> instance used to obtain a completion token for the runner, 
    /// null(default) means use a newly created instance
    /// </param>
    /// <param name="EnumAheadLimit">Size of a queue of items enumerated ahead but not fetched yet</param>
    /// <param name="PassSourceOnership">
    /// Flag showing that this  <see cref="AsyncEnumAdapterRunner{TResult}" /> is responsible for disposing the <paramref name="Source"/> value passed to it (defaults to true)
    /// </param>
    /// <param name="PassCtsOwnership">
    /// Flag showing that this  <see cref="AsyncEnumAdapterRunner{TResult}" /> is responsible for disposing 
    /// the <paramref name="CompletionTokenSource"/> value passed to it, if any (defaults to true)
    /// </param>
    /// <param name="StartInConstructor">Set to true to start fecthing data from a constructor</param>
    public record struct AsyncEnumAdapterParams<TResult>
    (
        IAsyncEnumerable<TResult> Source,
        int? DefaultAdvance = null,
        CancellationTokenSource? CompletionTokenSource = null,
        Int32? EnumAheadLimit = null,
        bool PassSourceOnership = true,
        bool PassCtsOwnership = true,
        Boolean StartInConstructor = false
    );
}
