namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Class containg parametrs to pass to the <see cref="TimeSeriesRunner{TResult}"/>  class
    /// <see cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(TimeSeriesParams{TResult}, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger{TimeSeriesRunner{TResult}}?)"> 
    /// constructor</see>.
    /// </summary>
    /// <typeparam name="TResult">
    /// Type specializing the structure. Must be the same as a type parameter of 
    /// the <see cref="TimeSeriesRunner{TResult}"/> class instance of which to be constructed.
    /// </typeparam>
    /// <param name="Gauge">
    /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Gauge"]'/>
    /// </param>
    /// <param name="Interval">
    /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Interval"]'/>
    /// </param>
    /// <param name="Count">
    /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Count"]'/>
    /// </param>
    /// <param name="DefaultAdvance">
    /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="DefaultAdvance"]'/>
    /// </param>
    /// <param name="CompletionTokenSource">
    /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="CompletionTokenSource"]'/>
    /// </param>
    /// <param name="EnumAheadLimit">
    /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="EnumAheadLimit"]'/>
    /// </param>
    /// <param name="PassCtsOwnership">
    /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="PassCtsOwnership"]'/>
    /// </param>
    /// <param name="StartInConstructor">
    /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="StartInConstructor"]'/>
    /// </param>
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
