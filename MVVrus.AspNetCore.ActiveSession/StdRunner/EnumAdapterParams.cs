using System.Collections.Concurrent;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Class containg parameters that are to be passed to the 
    /// <see cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(EnumAdapterParams{TItem}, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)"> 
    /// EnumAdapterRunner class constructor</see> while creating it via 
    /// <see cref="IActiveSession.CreateRunner{TRequest, TItem}(TRequest, HttpContext)">IActiveSession.CreateRunner</see> method.
    /// </summary>
    /// <typeparam name="TItem">Type specializing the runner's <see cref="IRunner{TItem}"/> interface</typeparam>
    /// <param name="Source">
    /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="Source"]'/>
    /// </param>
    /// <param name="DefaultAdvance">
    /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="DefaultAdvance"]'/>
    /// </param>
    /// <param name="CompletionTokenSource">
    /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="CompletionTokenSource"]'/>
    /// </param>
    /// <param name="EnumAheadLimit">
    /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="EnumAheadLimit"]'/>
    /// </param>
    /// <param name="PassSourceOnership">
    /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="PassSourceOnership"]'/>
    /// The default value is <see langword="true"/>
    /// </param>
    /// <param name="PassCtsOwnership">
    /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="PassCtsOwnership"]'/>
    /// </param>
    /// <param name="StartInConstructor">
    /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="StartInConstructor"]'/>
    /// </param>
    public record struct EnumAdapterParams<TItem>(
        IEnumerable<TItem> Source,
        int? DefaultAdvance = null,
        CancellationTokenSource? CompletionTokenSource = null,
        Int32? EnumAheadLimit=null,
        bool PassSourceOnership = true,
        bool PassCtsOwnership = true,
        Boolean StartInConstructor = false
    );
}
