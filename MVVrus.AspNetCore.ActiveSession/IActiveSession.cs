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
        /// <returns>A structure that contains the reference to the new runner and its number within the Active Session.</returns>
        KeyedRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context);

        /// <summary>
        /// A method used to search for an existing runner
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="RunnerNumber">An <see cref="Int32"/>  a number (key) specifying the runner to search for</param>
        /// <param name="Context"><see cref="HttpContext">Context</see> of the request from which the method is called</param>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        /// <returns>The runner with specified number and result type (<see cref="IRunner{TResult}"/>) if such a runner exists or null</returns>
        IRunner<TResult>? GetRunner<TResult>(int RunnerNumber, HttpContext Context);

        /// <summary>
        /// An asynchronous version of <see cref="GetRunner{TResult}(int, HttpContext)"/> method.
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="RunnerNumber">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <param name="Context"><see cref="HttpContext">Context</see> of the request from which the method is called</param>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        /// <param name="CancellationToken">Cancellation token that may be used to cancel this async operation</param>
        /// <returns>A <see cref="Task{T}"/> wrapping the existing runner (of type <see cref="IRunner{TResult}"/>) if any or null</returns>
        Task<IRunner<TResult>?> GetRunnerAsync<TResult>(
            int RunnerNumber,
            HttpContext Context, 
            CancellationToken CancellationToken = default
        );

        /// <summary>
        /// Terminate the active session aborting all runners. 
        /// </summary>
        /// <returns>The task that completes a cleanup (usally the same as CleanupCompletionTask property contains)</returns>
        /// <remarks>
        /// Session termination is synchronous but successive cleanup of runners may be asynchronous. 
        /// To await for completion of this operation use a task returned via CleanupCompletionTask property
        /// </remarks>
        Task Terminate(HttpContext Context);

        /// <summary>Indicator that the Active Session object is properly initialized and may be used.</summary>
        Boolean IsAvailable { get; }

        /// <summary>Indicator that the Active Session object was just created an is still empty.</summary>
        Boolean IsFresh { get; }

        /// <summary>The service (DI) container for the scope of the Active Session</summary>
        IServiceProvider SessionServices { get; }

        /// <summary>The ActiveSession identifier</summary>
        String Id { get; }

        /// <summary>
        /// A generation number of this ActiveSession within the containing <see cref="ISession">ASP.NET Core session</see>
        /// </summary>
        Int32 Generation {  get; }

        /// <summary>Cancellation token that will be fired after session completion (eviction/disposal)</summary>
        CancellationToken CompletionToken { get;}

        /// <summary>Task that performs asynchronous cleanup of this active session (waits for all runners completion etc).</summary>
        /// <remarks>
        /// The result of the task is false if cleanup of any runner has been not finished within a timeout, 
        /// otherwise the result is true
        /// If the asynchronous cleanup isn't used, it will be an aready completed task.
        /// </remarks>
        Task CleanupCompletionTask { get; }

    }
}
