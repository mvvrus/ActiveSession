using System.Collections.Concurrent;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Class containg parametrs to pass to a <see cref="EnumAdapterRunner{TResult}"/> constructor
    /// </summary>
    /// <typeparam name="TResult">Type specializing the runner's <see cref="IActiveSessionRunner{TResult}"/> interface</typeparam>
    /// <param name="AdapterBase">The base object implementing <see cref="IEnumerable{T}"/> for which the adapter to be created</param>
    /// <param name="Limit">Maximum number of elements acquired from the base and not returned to the caller yet</param>
    public record struct EnumAdapterParams<TResult> (
        IEnumerable<TResult> AdapterBase,
        Int32 Limit
    );
}
