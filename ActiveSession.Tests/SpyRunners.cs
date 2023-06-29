using Microsoft.Extensions.Primitives;

namespace ActiveSession.Tests
{
    public class SpyRunnerBase<TResult> : IActiveSessionRunner<TResult>
    {
        public ActiveSessionRunnerState State => throw new NotImplementedException();

        public int Position => throw new NotImplementedException();

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public ActiveSessionRunnerResult<TResult> GetAvailable(int StartPosition, Int32 Advance, String? TraceIdentifier)
        {
            throw new NotImplementedException();
        }

        public IChangeToken GetCompletionToken()
        {
            throw new NotImplementedException();
        }

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(int StartPosition, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}

