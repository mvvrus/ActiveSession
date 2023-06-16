using System.Threading.Tasks;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// An interface used to access Active Session object
    /// </summary>
    public interface IActiveSession
    {
        /// <summary>
        /// A method used to create a new runner
        /// </summary>
        /// <typeparam name="TRequest">Type of the initialization data used to create a new runner</typeparam>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="Request">Initialization data (of type <typeparamref name="TRequest"/>)</param>
        /// <param name="Context"><see cref="HttpContext"/> of the request this call is processing.</param>
        /// <returns>A <see cref="KeyedActiveSessionRunner{TResult}"/> record containng the runner reference and its key</returns>
        /// <remarks><paramref name="Context"/> parameter is now used just for tracing purposes</remarks>
        KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext? Context = null);

        /// <summary>
        /// A method used to search for an existing runner
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="Key">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <param name="Context"><see cref="HttpContext"/> of the request this call is processing.</param>
        /// <returns>The existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        /// <remarks><paramref name="Context"/> parameter is now used just for tracing purposes</remarks>
        IActiveSessionRunner<TResult>? GetRunner<TResult>(int Key, HttpContext? Context=null);

        /// <summary>
        /// An asynchronous version of <see cref="GetRunner{TResult}(int, HttpContext?)"/> method.
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="Key">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <param name="Context"><see cref="HttpContext"/> of the request this call is processing.</param>
        /// <param name="CancellationToken"></param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping the existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        /// <remarks><paramref name="Context"/> parameter is now used just for tracing purposes</remarks>
        ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            int Key, 
            HttpContext? Context=null, 
            CancellationToken CancellationToken = default
        );

        /// <value>Indicator that the Active Session object is properly initialized and may be used.</value>
        Boolean IsAvailable { get; }

        /// <value>Indicator that the Active Session object was just created an is still empty.</value>
        Boolean IsFresh { get; }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="CancellationToken"></param>
        /// <param name="TraceIdentifier"></param>
        /// <returns></returns>
#pragma warning disable CA1068 // CancellationToken parameters must come last
        Task CommitAsync(CancellationToken CancellationToken = default, String? TraceIdentifier = null ); 
#pragma warning restore CA1068 // CancellationToken parameters must come last
    }
}
