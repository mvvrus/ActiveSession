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
        /// <param name="Context"><see cref="HttpContext">Context</see> of the request from which the method is called</param>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        /// <returns>A <see cref="KeyedActiveSessionRunner{TResult}"/> record containng the runner reference and its key</returns>
        KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context);

        /// <summary>
        /// A method used to search for an existing runner
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="RunnerNumber">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <param name="Context"><see cref="HttpContext">Context</see> of the request from which the method is called</param>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        /// <returns>The existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        IActiveSessionRunner<TResult>? GetRunner<TResult>(int RunnerNumber, HttpContext Context);

        /// <summary>
        /// An asynchronous version of <see cref="GetRunner{TResult}(int, HttpContext)"/> method.
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="RunnerNumber">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <param name="Context"><see cref="HttpContext">Context</see> of the request from which the method is called</param>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        /// <param name="CancellationToken">Cancellation token that may be used to cancel this async operation</param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping the existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            int RunnerNumber,
            HttpContext Context, 
            CancellationToken CancellationToken = default
        );

        /// <summary>
        /// Terminate the active session aborting all runners. 
        /// </summary>
        /// <param name="Global">If true terminate all ActiveSessions with the same id on all nodes (currently ignored)</param>
        /// <remarks>
        /// This operation may be asynchronous. 
        /// To await for completion of this operation use a task returned via CleanupCompletionTask property
        /// </remarks>
        void Terminate(Boolean Global = false);

        /// <value>Indicator that the Active Session object is properly initialized and may be used.</value>
        Boolean IsAvailable { get; }

        /// <value>Indicator that the Active Session object was just created an is still empty.</value>
        Boolean IsFresh { get; }

        /// <value>The service (DI) container for the scope of the Active Session</value>
        IServiceProvider SessionServices { get; }

        /// <value>The ActiveSession identifier</value>
        String Id { get; }

        /// <value>Cancellation token that will be fired after session completion (eviction/disposal)</value>
        CancellationToken CompletionToken { get;}

        /// <value>Task that performs asynchronous cleanup of this active session (waits for all runners completion etc).</value>
        /// <remarks>If the asynchronous cleanup isn't used, it will be an aready completed task.</remarks>
        Task CleanupCompletionTask { get; }

    }
}
