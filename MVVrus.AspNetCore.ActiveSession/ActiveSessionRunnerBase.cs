using Microsoft.Extensions.Primitives;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO write documentation
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public abstract class ActiveSessionRunnerBase<TResult> : IActiveSessionRunner<TResult>
    {
        /// <inheritdoc/>
        public abstract ActiveSessionRunnerState State { get; }

        /// <inheritdoc/>
        public abstract Int32 Position { get; }

        /// <inheritdoc/>
        public abstract void Abort();

        /// <inheritdoc/>
        public abstract ActiveSessionRunnerResult<TResult> GetAvailable(Int32 StartPosition = -1, Int32 Advance = int.MaxValue, String? TraceIdentifier = null);

        /// <inheritdoc/>
        public abstract IChangeToken GetCompletionToken();

        /// <inheritdoc/>
        public abstract ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier = null, CancellationToken Token = default);
    }
}
