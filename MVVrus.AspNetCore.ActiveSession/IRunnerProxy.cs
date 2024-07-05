namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Extension of <see cref="IRunner"/> to support remote runner calls. Not implemented yet
    /// </summary>
    public interface IRunnerProxy : IRunner
    {
        /// <summary>
        /// Indicates that the runner execute itself on the same host as a caller.
        /// </summary>
        public Boolean IsLocal { get; }

        /// <summary>
        /// Asynchronous counterpart of the <see cref="IRunner.Status"/> property.
        /// </summary>
        /// <param name="Token">Can be used for cooperative cancellation 
        /// of the operation initiated by this method (i.e. cancellation of the result task).</param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a value of the <see cref="IRunner.Status"/> property (possibly from a remote host) </returns>
        public ValueTask<RunnerStatus> GetStatusAsync(CancellationToken Token=default);

        /// <summary>
        /// Asynchronous counterpart of the <see cref="IRunner.Position"/> property.
        /// </summary>
        /// <param name="Token"><inheritdoc cref="GetStatusAsync(CancellationToken)" pah='/param[@name="Token"]'/></param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a value of the <see cref="IRunner.Position"/> property (possibly obtained from a remote host). </returns>
        public ValueTask<Int32> GetPositionAsync(CancellationToken Token = default);

        /// <summary>
        /// Asynchronous counterpart of the <see cref="IRunner.Exception"/> property.
        /// </summary>
        /// <param name="Token"><inheritdoc cref="GetStatusAsync(CancellationToken)" pah='/param[@name="Token"]'/></param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a value of the <see cref="IRunner.Exception"/> property (possibly from a remote host) </returns>
        public ValueTask<Exception?> GetExceptionAsync(CancellationToken Token = default);

        /// <summary>
        /// Asynchronous counterpart of the <see cref="IRunner.Abort"/> method.
        /// </summary>
        /// <param name="TraceIdentifier"><inheritdoc path='/param[@name="TraceIdentifier"]' cref="IRunner.Abort(string?)" /></param>
        /// <param name="Token"><inheritdoc cref="GetStatusAsync(CancellationToken)" pah='/param[@name="Token"]'/></param>
        /// <returns>A task representing an asynchronous call of the <see cref="IRunner.Abort(string?)"/> method (possibly on a remote host).
        /// </returns>
        public Task AbortAsync(String? TraceIdentifier = null, CancellationToken Token = default);

        /// <summary>
        /// Asynchronous counterpart of the <see cref="IRunner.GetProgress"/> method.
        /// </summary>
        /// <param name="Token"><inheritdoc cref="GetStatusAsync(CancellationToken)" pah='/param[@name="Token"]'/></param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a result returned by <see cref="IRunner.GetProgress"/> method (possibly from a remote host) </returns>
        ValueTask<RunnerBkgProgress> GetProgressAsync(CancellationToken Token = default);

        /// <summary>
        /// Asynchronous counterpart of the <see cref="IRunner.IsBackgroundExecutionCompleted"/> property.
        /// </summary>
        /// <param name="Token"><inheritdoc cref="GetStatusAsync(CancellationToken)" pah='/param[@name="Token"]'/></param>
        /// <returns>A <see cref="ValueTask{T}"/> wrapping a value of the <see cref="IRunner.IsBackgroundExecutionCompleted"/> property (possibly from a remote host) </returns>
        ValueTask<Boolean> GetIsBackgroundExecutionCompletedAsync(CancellationToken Token = default);

    }

    /// <summary>
    /// Extension of <see cref="IRunner{TResult}"/> to support remote runner calls. Not implemented yet
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IRunnerProxy<TResult>:IRunner<TResult>, IRunnerProxy
    {
        /// <summary>
        /// Asynchronous counterpart of the <see cref="IRunner{TResult}.GetAvailable(int, int, String?)"/> method.
        /// </summary>
        /// <param name="Advance"><inheritdoc path='/param[@name="Advance"]' cref="IRunner{TResult}.GetAvailable(int, int, string?)" /></param>
        /// <param name="StartPosition"><inheritdoc path='/param[@name="StartPosition"]' cref="IRunner{TResult}.GetAvailable(int, int, string?)" /></param>
        /// <param name="TraceIdentifier"><inheritdoc path='/param[@name="TraceIdentifier"]' cref="IRunner{TResult}.GetAvailable(int, int, string?)" /></param>
        /// <param name="Token"><inheritdoc cref="IRunnerProxy.GetStatusAsync(CancellationToken)" pah='/param[@name="Token"]'/></param>
        /// <returns> 
        /// A <see cref="ValueTask{T}"/> wrapping a value returned by 
        /// <see cref="IRunner{TResult}.GetAvailable(int, int, String?)"/> method call (possibly from a remote host) 
        /// </returns>
        public ValueTask<RunnerResult<TResult>> GetAvailableAsync(Int32 Advance = MAXIMUM_ADVANCE, Int32 StartPosition = CURRENT_POSITION, String? TraceIdentifier = null, CancellationToken Token = default);

    }
}
