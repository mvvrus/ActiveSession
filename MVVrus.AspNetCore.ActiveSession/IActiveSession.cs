using System.Threading.Tasks;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// An interface used to access Active Session object
    /// </summary>
    public interface IActiveSession: ILocalSession
    {
        /// <summary>
        /// A method used to create a new runner
        /// </summary>
        /// <typeparam name="TRequest">Type of the data used to create a new runner.</typeparam>
        /// <typeparam name="TResult">Type of the result, returned by the runner.</typeparam>
        /// <param name="Request">Data to be passed as a parameter to the runner's constructor to create the runner. </param>
        /// <param name="Context">Context of the HTTP request from a handler of which the method is called.</param>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        /// <returns>A structure that contains the reference to the new runner and its number within the active session.</returns>
        KeyedRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context);

        /// <summary>
        /// A method used to search for an existing runner
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="RunnerNumber">A number (key) specifying the runner to search for</param>
        /// <param name="Context">Context of the HTTP request from handler of which the method is called</param>
        /// <returns>The runner with specified number and result type (<see cref="IRunner{TResult}"/>) if such a runner exists or null</returns>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        IRunner<TResult>? GetRunner<TResult>(int RunnerNumber, HttpContext Context);

        /// <summary>
        /// An asynchronous version of <see cref="GetRunner{TResult}(int, HttpContext)"/> method.
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="RunnerNumber"><inheritdoc cref="GetRunner{TResult}(int, HttpContext)" path='/param[@name="RunnerNumber"]' /></param>
        /// <param name="Context"><inheritdoc cref="GetRunner{TResult}(int, HttpContext)" path='/param[@name="Context"]' /></param>
        /// <param name="CancellationToken">Cancellation token that may be used to cancel this async operation</param>
        /// <returns>A task wrapping the existing runner (of type <see cref="IRunner{TResult}"/>) if any or null</returns>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        ValueTask<IRunner<TResult>?> GetRunnerAsync<TResult>(
            int RunnerNumber,
            HttpContext Context, 
            CancellationToken CancellationToken = default
        );

        /// <summary>
        /// A method used to search for and return a base, non-typed interface of an existing runner
        /// </summary>
        /// <param name="RunnerNumber"><inheritdoc cref="GetRunner{TResult}(int, HttpContext)" path='/param[@name="RunnerNumber"]' /></param>
        /// <param name="Context"><inheritdoc cref="GetRunner{TResult}(int, HttpContext)" path='/param[@name="Context"]' /></param>
        /// <returns>A base, non-typed runner interface.</returns>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        IRunner? GetNonTypedRunner(int RunnerNumber, HttpContext Context);

        /// <summary>
        /// An asynchronous version of <see cref="GetNonTypedRunner(int, HttpContext)"/> method.
        /// </summary>
        /// <param name="RunnerNumber"><inheritdoc cref="GetRunner{TResult}(int, HttpContext)" path='/param[@name="RunnerNumber"]' /></param>
        /// <param name="Context"><inheritdoc cref="GetRunner{TResult}(int, HttpContext)" path='/param[@name="Context"]' /></param>
        /// <param name="CancellationToken">
        /// <inheritdoc cref="GetRunnerAsync{TResult}(int, HttpContext, CancellationToken)" path='/param[@name="CancellationToken"]' />
        /// </param>
        /// <returns>A task wrapping a base, non-typed interface of the existing runner (of type <see cref="IRunner"/>) if any or null</returns>
        /// <remarks><paramref name="Context"/> parameter is used here just for tracing purposes</remarks>
        ValueTask<IRunner?> GetNonTypedRunnerAsync(
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

        /// <summary>Indicator that the Active Session object was just created an is still empty.</summary>
        Boolean IsFresh { get; }

        /// <summary>The ActiveSession identifier.</summary>
        String Id { get; }

        /// <summary>
        /// A generation number of this ActiveSession within the containing <see cref="ISession">ASP.NET Core session/</see>
        /// </summary>
        Int32 Generation {  get; }

        /// <summary>Cancellation token that will be fired after the active session completion (eviction/disposal) at start of cleanup/</summary>
        CancellationToken CompletionToken { get;}

        /// <summary>Task that performs asynchronous cleanup of this active session (waits for all runners completion etc).</summary>
        /// <remarks>
        /// The result of the task is false if cleanup of any runner has been not finished within a timeout, 
        /// otherwise the result is true
        /// If the asynchronous cleanup isn't used, it will be an aready completed task.
        /// </remarks>
        Task CleanupCompletionTask { get; }

        /// <summary>
        /// Indicates whether no runners are executing within this session.
        /// </summary>
        /// <remarks> In this version of the library this property is a stub 
        /// just returning a value of <see cref="IsFresh"/> property./</remarks>
        Boolean IsIdle { get; }

        /// <summary>
        /// Waits until all runners in the session complete their execution.
        /// </summary>
        /// <param name="AbortAll">Indicates whether all executing runners should be immediately aborted.</param>
        /// <param name="Timeout">A timeout value for waiting.</param>
        /// <param name="Token">Can be used to cancel wait</param>
        /// <returns>A task representing a wait process. The task result is <see langword="true"/> if the wait was successfull.</returns>
        /// <remarks> In this version of the library this method is a stub
        /// just returning immediately with the result set to a value of <see cref="IsFresh"/> property.</remarks>
        ValueTask<Boolean> WaitUntilIdle(Boolean AbortAll, TimeSpan Timeout, CancellationToken Token=default);

        /// <summary>
        /// Obtains a task for the runner that tracks the runner's completion and cleanup, 
        /// completing after the runner has been completed and its object has been disposed (if disposal is required).
        /// </summary>
        /// <param name="RunnerNumber">An <see cref="Int32"/>A key specifying the runner for which the task is obtained.</param>
        /// <returns>The task tracking cleanup of the runner specified by  <paramref name="RunnerNumber"/> or null if no such runner exists.</returns>
        public Task? TrackRunnerCleanup(Int32 RunnerNumber);

    }
}
