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
        /// <returns>A <see cref="KeyedActiveSessionRunner{TResult}"/> record containng the runner reference and its key</returns>
        KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request);

        /// <summary>
        /// A method used to search for an existing runner
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="Key">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <returns>The existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        IActiveSessionRunner<TResult>? GetRunner<TResult>(int Key);

        /// <summary>
        /// An asynchronous version of <see cref="GetRunner{TResult}(int)"/> method.
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="Key">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping the existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            int Key, 
            CancellationToken cancellationToken = default
        );

        /// <value>Indicator that the Active Session object is properly initialized and may be used.</value>
        Boolean IsAvailable { get; }

        /// <value>Indicator that the Active Session object was just created an is still empty.</value>
        Boolean IsFresh { get; }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="CancellationToken"></param>
        /// <returns></returns>
        Task CommitAsync(CancellationToken CancellationToken = default); 
    }
}
