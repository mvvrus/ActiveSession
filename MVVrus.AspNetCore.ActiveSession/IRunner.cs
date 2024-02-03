using Microsoft.Extensions.Primitives;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains collection of common, type-indendent constants, properies and methods 
    /// for a generic runner ( IRunner{TResult} ) interface
    /// </summary>
    public interface IRunner
    {
        /// <value>
        /// Constant indicating the start position, from which the runner execution begins
        /// </value>
        public const Int32 BEGINNING_POSITION = 0;
        /// <value>
        /// Constant indicating the use of current <see cref="IRunner.Position"/>, as a fetch start position
        /// </value>
        public const Int32 CURRENT_POSITION = -1;
        /// <value>
        /// Constant indicating the use of default value as a desired advance of a <see cref="IRunner.Position"></see>.
        /// </value>
        public const Int32 DEFAULT_ADVANCE = 0;

        /// <value>
        /// Constant indicating the use of maximum available value as a desired advance of a <see cref="IRunner.Position"></see>.
        /// </value>
        public const Int32 MAXIMUM_ADVANCE = Int32.MaxValue;

        /// <value>
        /// Current state of the runner object
        /// </value>
        public RunnerStatus State { get; }

        /// <value>
        /// Current position of the runner object
        /// </value>
        public Int32 Position { get; }

        /// <value>
        /// Method that terminates the runner execution
        /// </value>
        public void Abort();

        /// <value>
        /// CancellationToken to be cancelled then runner is completed:
        /// fetched all data and passed it to a caller, aborted or failed with an exception
        /// </value>
        public CancellationToken CompletionToken { get; }

        /// <value>
        /// The exception that cause the <see cref="State"/> to change to <see cref="RunnerStatus.Failed"/>, otherwise - null
        /// </value>
        public Exception? Exception { get; }

        /// <value>
        /// Runnner identifier (see <see cref="RunnerId"/>) if exposed by the runner and assigned, otherwise - default(RunnerId)
        /// </value>
        public RunnerId Id { get=>default; }
    }

    /// <summary>
    /// Generic ActiveSession runner interface
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the interface methods</typeparam>
    /// <remarks>
    /// Inherited from non-generic <see cref="IRunner"></see> interface 
    /// that contains properties and methosds independent of the type of the result.
    /// </remarks>
    public interface IRunner<TResult>:IRunner
    {
        /// <summary>
        /// Asynchronously fetch the next (or first) results from the runner
        /// </summary>
        /// <param name="StartPosition">
        /// Position value from which the fetching of the results should begin
        /// <remarks> 
        /// <para>Use <see cref="IRunner.BEGINNING_POSITION"/> constant to fetch results from the very begining.</para>
        /// <para>Use <see cref="IRunner.CURRENT_POSITION"/> constant to continue to fetch results from the last position fetched.</para>
        /// <para>For implementers: it's recomended not to fetch anything if this value differs from the current runner's <see cref="IRunner.Position">Position property</see>value</para>
        /// </remarks>
        /// </param>
        /// <param name="Advance">Desired increment of the runner's <see cref="IRunner.Position"/>, at which the fetch should stop</param>
        /// <param name="TraceIdentifier">Control flow identifier used for tracing</param>
        /// <param name="Token">
        /// <see cref="CancellationToken"/> that may be used to break the method execution.
        /// <remarks>
        /// Breaking the method execution means that the method stops at the point of break 
        /// and return a result taken up to the point
        /// </remarks>
        /// </param>
        /// <returns> 
        /// A task returning an <see cref="RunnerResult{TResult}"/> value containing the state, the position of the runner at the point of completion 
        /// and the result (of type <typeparamref name="TResult"/>) if any
        /// </returns>
        public ValueTask<RunnerResult<TResult>> GetRequiredAsync(
            Int32 StartPosition, 
            Int32 Advance, 
            String? TraceIdentifier=null, 
            CancellationToken Token=default
        );

        /// <summary>
        /// Returns the result available at the moment of call starting from <see paramref="StartPosition"/>
        /// </summary>
        /// <param name="StartPosition">
        /// Position value from which the fetching of the results should begin
        /// <remarks> 
        /// <para>Use <see cref="IRunner.BEGINNING_POSITION"/> constant to fetch results from the very begining.</para>
        /// <para>Use <see cref="IRunner.CURRENT_POSITION"/> constant to continue to fetch results from the last position fetched.</para>
        /// <para>For implementers: it's recomended not to fetch anything if this value differs from the current runner's <see cref="IRunner.Position">Position property</see>value</para>
        /// </remarks>
        /// </param>
        /// <param name="Advance">Desired increment of the runner's <see cref="IRunner.Position"/>, at which the fetch should stop</param>
        /// <param name="TraceIdentifier">Control flow identifier used for tracing</param>
        /// An <see cref="RunnerResult{TResult}"/> value containing the state, the position of the runner 
        /// at the point of completion and the result (of type <typeparamref name="TResult"/>) if any
        public RunnerResult<TResult> GetAvailable(Int32 StartPosition= CURRENT_POSITION, Int32 Advance=MAXIMUM_ADVANCE, String? TraceIdentifier = null);
    }
}
