namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Class containg parametrs to pass to a <see cref="TimeSeriesRunner{TResult}"/> constructor
    /// </summary>
    /// <typeparam name="TResult">Type specializing the runner's <see cref="IRunner{TResult}"/> interface</typeparam>
    /// <param name="Gauge">TODO</param>
    /// <param name="Interval">TODO</param>
    /// <param name="Count">TODO</param>
    /// <param name="DefaultAdvance">
    /// Maximum number of elements in a series to be returned to the caller in one call (currently defaults to 20)
    /// </param>
    /// <param name="CompletionTokenSource">
    /// An external <see cref="CancellationTokenSource"/> instance used to obtain a completion token for the runner, 
    /// null(default) means use a newly created instance
    /// </param>
    /// <param name="EnumAheadLimit">Size of a queue of items enumerated ahead but not fetched yet</param>
    /// <param name="PassCtsOwnership">
    /// Flag showing that this  <see cref="AsyncEnumAdapterRunner{TResult}" /> is responsible for disposing 
    /// the <paramref name="CompletionTokenSource"/> value passed to it, if any (defaults to true)
    /// </param>
    /// <param name="StartInConstructor">Set to true to start fecthing data from a constructor</param>
    public record struct TimeSeriesParams<TResult>
    (
        Func<TResult> Gauge, 
        TimeSpan Interval, 
        Int32? Count, 
        int? DefaultAdvance = null,
        CancellationTokenSource? CompletionTokenSource = null,
        Int32? EnumAheadLimit = null,
        bool PassCtsOwnership = true,
        Boolean StartInConstructor = false
    );
}
