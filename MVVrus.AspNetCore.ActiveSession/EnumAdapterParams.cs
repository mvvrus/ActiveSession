using System.Collections.Concurrent;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Class containg parametrs to pass to a <see cref="EnumAdapterRunner{TResult}"/> constructor
    /// </summary>
    /// <typeparam name="TResult">Type specializing the runner's <see cref="IActiveSessionRunner{TResult}"/> interface</typeparam>
    /// <param name="AdapterBase">The base object implementing <see cref="IEnumerable{T}"/> for which the adapter to be created</param>
    /// <param name="Limit">Maximum number of elements acquired from the base returned to the caller (defaults to 1)</param>
    /// <param name="PassAdapterBaseOnership">
    /// Flag showing that the  <see cref="EnumAdapterRunner{TResult}" /> is responsible for disposing the
    /// <paramref name="AdapterBase"/> value passed to it (defaults to true)
    /// </param>
    public record struct EnumAdapterParams<TResult> (
        IEnumerable<TResult> AdapterBase,
        Int32 Limit = 1,
        Boolean PassAdapterBaseOnership =true
    );
}
