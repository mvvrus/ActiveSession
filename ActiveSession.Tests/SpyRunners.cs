namespace ActiveSession.Tests
{
    public class SpyRunnerBase<TResult> : IActiveSessionRunner<TResult>
    {
        public ActiveSessionRunnerState State => throw new NotImplementedException();

        public int Position => throw new NotImplementedException();

        public ActiveSessionRunnerResult<TResult> GetAvailable(int StartPosition)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(int StartPosition, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}

