using Microsoft.Extensions.Primitives;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// A collection of common, type-indendent properies and methods of generic a runner ( IActiveSessionRunner{TResult} ) interface
    /// </summary>
    public interface IActiveSessionRunner
    {
        /// <value>
        /// Current state of the runner object
        /// </value>
        public ActiveSessionRunnerState State { get; }

        /// <value>
        /// Current position of the runner object
        /// </value>
        Int32 Position { get; }

        /// <value>
        /// Method that terminates the runner execution
        /// </value>
        public void Abort();

        /// <value>
        /// Method that acquires IChangeToken signalling about the r
        /// </value>
        IChangeToken GetCompletionToken();
    }

    /// <summary>
    /// Generic ActiveSession runner interface
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the interface methods</typeparam>
    /// <remarks>
    /// Inherited from non-generic <see cref="IActiveSessionRunner"></see> interface 
    /// that contains properties and methosds independent of the type of the result.
    /// </remarks>
    public interface IActiveSessionRunner<TResult>:IActiveSessionRunner
    {
        /// <value>
        /// Constant indicating the start position, from which the runner execution begins
        /// </value>
        public const Int32 BEGINNING = 0;
        /// <value>
        /// Constant indicating the use of default value as a desired advance of a <see cref="IActiveSessionRunner.Position"></see>.
        /// </value>
        public const Int32 DEFAULT_ADVANCE=0;

        /// <summary>
        /// Asynchronously fetch the next (or first) results from the runner
        /// </summary>
        /// <param name="StartPosition">
        /// Position value from which the fetching of the results should begin
        /// <remarks> 
        /// <para>Use BEGINNING constant to fetch results from the very begining.</para>
        /// <para>If concurent calls of the method are possible it's recomended not to fetch anything if this value differs from the current runner Position value</para>
        /// </remarks>
        /// </param>
        /// <param name="Advance">Desired increment of the runner's Position, at which the fetch should stop</param>
        /// <param name="token"><see cref="CancellationToken"/> that may be used to cancel the method execution</param>
        /// <returns> 
        /// A task returning an <see cref="ActiveSessionRunnerResult{TResult}"/> value containing the state, the position of the runner at the point of completion 
        /// and the result (of type <typeparamref name="TResult"/>) if any
        /// </returns>
        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync(Int32 StartPosition, Int32 Advance, CancellationToken token=default);

        //TODO use <inhqeritdoc>
        /// <summary>
        /// A simplified form of <see cref="GetMoreAsync(int, int, CancellationToken)"></see> method always used the default value for an Advance parameter
        /// </summary>
        /// <param name="StartPosition">
        /// Position value from which the fetching of the results should begin
        /// <remarks> 
        /// <para>Use BEGINNING constant to fetch results from the very begining.</para>
        /// <para>If concurent calls of the method are possible it's recomended not to fetch anything if this value differs from the current runner Position value</para>
        /// </remarks>
        /// </param>
        /// <param name="token"><see cref="CancellationToken"/> that may be used to cancel the method execution</param>
        /// <returns> 
        /// A task returning an <see cref="ActiveSessionRunnerResult{TResult}"/> value containing the state, the position of the runner at the point of completion 
        /// and the result (of type <typeparamref name="TResult"/>) if any
        /// </returns>
        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync(Int32 StartPosition, CancellationToken token = default)
        {
            return GetMoreAsync(StartPosition, DEFAULT_ADVANCE, token);
        }

        /// <summary>
        /// Returns the result available at the moment of call starting from <see paramref="StartPosition"/>
        /// </summary>
        /// <param name="StartPosition">
        /// Position value from which the fetching of the results should begin
        /// </param>
        /// <remarks>TODO?</remarks>
        /// <returns>TODO</returns>
        public ActiveSessionRunnerResult<TResult> GetAvailable(Int32 StartPosition);
    }
}
