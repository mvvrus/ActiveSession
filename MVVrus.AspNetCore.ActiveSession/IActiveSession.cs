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
        /// <param name="TraceIdentifier">Control flow identifier used for tracing</param>
        /// <returns>A <see cref="KeyedActiveSessionRunner{TResult}"/> record containng the runner reference and its key</returns>
        KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, String? TraceIdentifier = null);

        /// <summary>
        /// A method used to search for an existing runner
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="RunnerNumber">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <param name="TraceIdentifier">Control flow identifier used for tracing</param>
        /// <returns>The existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        IActiveSessionRunner<TResult>? GetRunner<TResult>(int RunnerNumber, String? TraceIdentifier=null);

        /// <summary>
        /// An asynchronous version of <see cref="GetRunner{TResult}(int, String?)"/> method.
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="RunnerNumber">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <param name="TraceIdentifier">Control flow identifier used for tracing</param>
        /// <param name="CancellationToken">Cancellation token that may be used to cancel this async operation</param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping the existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            int RunnerNumber, 
            String? TraceIdentifier= null, 
            CancellationToken CancellationToken = default
        );

        /// <value>Indicator that the Active Session object is properly initialized and may be used.</value>
        Boolean IsAvailable { get; }

        /// <value>Indicator that the Active Session object was just created an is still empty.</value>
        Boolean IsFresh { get; }

        /// <value>The service (DI) container for the scope of the Active Session</value>
        IServiceProvider SessionServices { get; }

        /// <summary>
        /// Commit ActiveSession-related information into a storage
        /// </summary>
        /// <param name="TraceIdentifier">Control flow identifier used for tracing</param>
        /// <param name="CancellationToken">Cancellation token that may be used to cancel this async operation</param>
        /// <returns></returns>
        Task CommitAsync(String? TraceIdentifier = null, CancellationToken CancellationToken = default);

        
        /// <value>The ActiveSession identifier</value>
        String Id { get; }
    }
}
