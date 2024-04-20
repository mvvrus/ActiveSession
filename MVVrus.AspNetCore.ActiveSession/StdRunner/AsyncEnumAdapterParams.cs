namespace MVVrus.AspNetCore.ActiveSession.StdRunner 
{
    /// <summary>
    /// Class containg parameters that are to be passed to the 
    /// <see cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)"> 
    /// AsyncEnumAdapterRunner class constructor</see> while creating it via 
    /// <see cref="IActiveSession.CreateRunner{TRequest, TItem}(TRequest, HttpContext)">IActiveSession.CreateRunner</see> method.
    /// </summary>
    /// <typeparam name="TItem">Type specializing the runner's <see cref="IRunner{TItem}"/> interface</typeparam>
    /// <param name="Source">
    /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Source"]'/>
    /// </param>
    /// <param name="DefaultAdvance">
    /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="DefaultAdvance"]'/>
    /// </param>
    /// <param name="CompletionTokenSource">
    /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="CompletionTokenSource"]'/>
    /// </param>
    /// <param name="EnumAheadLimit">
    /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="EnumAheadLimit"]'/>
    /// </param>
    /// <param name="PassSourceOnership">
    /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="PassSourceOnership"]'/>
    /// The default value is <see langword="true"/>
    /// </param>
    /// <param name="PassCtsOwnership">
    /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="PassCtsOwnership"]'/>
    /// </param>
    /// <param name="StartInConstructor">
    /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="StartInConstructor"]'/>
    /// </param>
    public record struct AsyncEnumAdapterParams<TItem>
    (
        IAsyncEnumerable<TItem> Source,
        int? DefaultAdvance = null,
        CancellationTokenSource? CompletionTokenSource = null,
        Int32? EnumAheadLimit = null,
        bool PassSourceOnership = true,
        bool PassCtsOwnership = true,
        Boolean StartInConstructor = false
    );
}
