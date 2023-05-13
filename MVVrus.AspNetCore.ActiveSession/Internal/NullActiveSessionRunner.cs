namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSessionRunner<TResult> : IActiveSessionRunner<TResult>
    {
        public ActiveSessionRunnerState State => ActiveSessionRunnerState.Complete;

        public int Position => IActiveSessionRunner<TResult>.BEGINNING;

        public ActiveSessionRunnerResult<TResult> GetAvailable(int StartPosition)
        {
            return default;
        }

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(int StartPosition, CancellationToken token = default)
        {
            return new ValueTask<ActiveSessionRunnerResult<TResult>>(Task.FromResult<ActiveSessionRunnerResult<TResult>>(default));
        }
    }
}
