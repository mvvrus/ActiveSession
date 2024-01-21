using System.Collections.Concurrent;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Class containg parametrs to pass to a <see cref="EnumAdapterRunner{TResult}"/> constructor
    /// </summary>
    /// <typeparam name="TResult">Type specializing the runner's <see cref="IRunner{TResult}"/> interface</typeparam>
    /// <param name="Source">The base object implementing <see cref="IEnumerable{T}"/> for which the adapter to be created</param>
    /// <param name="DefaultAdvance">
    /// Maximum number of elements acquired from the <paramref name="Source"/> 
    /// to be returned to the caller in one call(defaults to 1)
    /// </param>
    /// <param name="CompletionTokenSource">
    /// An external <see cref="CancellationTokenSource"/> instance used to obtain a completion token for the runner, 
    /// null(default) means use a newly created instance
    /// </param>
    /// <param name="EnumAheadLimit">Size of a queue of items enumerated ahead but not fetched yet</param>
    /// <param name="PassSourceOnership">
    /// Flag showing that the  <see cref="EnumAdapterRunner{TResult}" /> is responsible for disposing the <paramref name="Source"/> value passed to it (defaults to true)
    /// </param>
    /// <param name="PassCtsOwnership">
    /// Flag showing that the  <see cref="EnumAdapterRunner{TResult}" /> is responsible for disposing 
    /// the <paramref name="CompletionTokenSource"/> value passed to it, if any (defaults to true)
    /// </param>
    public record struct EnumAdapterParams<TResult>(
        IEnumerable<TResult> Source,
        int? DefaultAdvance = null,
        CancellationTokenSource? CompletionTokenSource = null,
        Int32? EnumAheadLimit=null,
        bool PassSourceOnership = true,
        bool PassCtsOwnership = true
    );
}
