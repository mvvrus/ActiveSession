using Microsoft.Extensions.Primitives;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains collection of common, type-indendent constants, properies and methods 
    /// for a generic runner ( IRunner{TResult} ) interface
    /// </summary>
    public interface IRunner
    {
        /// <summary>
        /// Constant indicating the use of current <see cref="IRunner.Position"/>, as a fetch start position.
        /// </summary>
        public const Int32 CURRENT_POSITION = -1;
        /// <summary>
        /// Constant indicating the use of default value as a desired advance of a <see cref="IRunner.Position"/>.
        /// </summary>
        public const Int32 DEFAULT_ADVANCE = 0;

        /// <summary>
        /// Constant indicating the use of maximum available value as a desired advance of a <see cref="IRunner.Position"/>.
        /// </summary>
        public const Int32 MAXIMUM_ADVANCE = Int32.MaxValue;

        /// <summary>
        /// Current status of the runner object.
        /// </summary>
        public RunnerStatus Status { get; }

        /// <summary>
        /// Current position of the runner object.
        /// </summary>
        /// <remarks>Precise meaning of this property is determined by specific runners</remarks>
        public Int32 Position { get; }

        /// <summary>
        /// Method that terminates the runner execution.
        /// </summary>
        /// <param name="TraceIdentifier">Sting that can be used for tracing.</param>
        /// <remarks>If this string is specified it is placed to log records emitted from a runner.</remarks>
        public void Abort(String? TraceIdentifier = null);

        /// <summary>
        /// The CancellationToken that will be cancelled then the runner execution is completed.
        /// </summary>
        /// <remarks>Used by ActiveSession library infrastructure and possibly by an application.</remarks>
        public CancellationToken CompletionToken { get; }

        /// <summary>
        /// The exception that causes the runner to come to the <see cref="RunnerStatus.Failed"/> status, otherwise - null.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// The runner identifier. 
        /// </summary>
        /// <remarks>Contains the default value of <see cref="RunnerId"/> type if not exposed by the runner or assigned in a constructor.</remarks>
        public RunnerId Id { get=>default; }
    }

    /// <summary>
    /// A generic interface that must be implemented by an ActiveSession runner.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the interface methods.</typeparam>
    /// <remarks>
    /// Inherited from non-generic <see cref="IRunner"></see> interface 
    /// that contains properties and methosds independent of the type of the result.
    /// </remarks>
    public interface IRunner<TResult>:IRunner
    {
        /// <summary>
        /// <toinherit>Asynchronously fetch result from the runner up to the specified point.</toinherit>
        /// </summary>
        /// <param name="Advance">Desired maximum increment of the runner's <see cref="IRunner.Position"/>, at which the fetch should stop.
        /// Interpretaion of the default parameter value <see cref="IRunner.DEFAULT_ADVANCE">DEFAULT_ADVANCE</see> depends on a type of the runner.
        /// </param>
        /// <param name="StartPosition"><inheritdoc cref="GetAvailable(int, int, string?)" path='/param[@name="StartPosition"]/node()'/></param>
        /// <param name="TraceIdentifier"><inheritdoc cref="GetAvailable(int, int, string?)" path='/param[@name="TraceIdentifier"]'/></param>
        /// <param name="Token">
        /// A CancellationToken that may be used to cancel the returned ValueTask.
        /// Cancellation typically does not affect a background runner execution.
        /// </param>
        /// <returns>
        /// <toinherit>
        /// A ValueTask that has a result of type <see cref="RunnerResult{TResult}"/>. 
        /// </toinherit>
        /// <inheritdoc cref="GetAvailable(int, int, string?)" path='/returns/*'/>
        /// </returns>
        public ValueTask<RunnerResult<TResult>> GetRequiredAsync(
            Int32 Advance = DEFAULT_ADVANCE,
            CancellationToken Token = default,
            Int32 StartPosition =CURRENT_POSITION,
            String? TraceIdentifier=null
        );

        /// <summary>
        /// <toinherit>Returns a result of the runner available at the moment of the method call.</toinherit>
        /// </summary>
        /// <param name="Advance">Desired increment of the runner's <see cref="IRunner.Position"/>, at which the fetch should stop.
        /// If the backgound process did not get so far, this method returns the result for the last Position reached.
        /// </param>
        /// <param name="StartPosition">
        /// <toinherit>Position value from which a fetch of the result should begin. </toinherit>
        /// Use <see cref="IRunner.CURRENT_POSITION"/> constant to continue to fetch result from the last position fetched.
        /// </param>
        /// <param name="TraceIdentifier"> <inheritdoc cref="IRunner.Abort" path='/param[@name="TraceIdentifier"]'/>
        /// </param>
        /// <returns>
        /// <toinherit>
        /// Fields in the structure returned as a result contains values of the properties 
        /// <see cref="IRunner.Status"/> and <see cref="IRunner.Position"/>
        /// at the point of completion in fields with the same names 
        /// and a runner-specific result in a <see cref="RunnerResult{TResult}.Result"/> field,
        /// </toinherit>
        /// the field Result type being TResult).
        /// </returns>
        public RunnerResult<TResult> GetAvailable(Int32 Advance = MAXIMUM_ADVANCE, Int32 StartPosition = CURRENT_POSITION, String? TraceIdentifier = null);
    }
}
