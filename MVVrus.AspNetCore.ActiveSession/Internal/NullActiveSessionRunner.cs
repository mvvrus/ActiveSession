using Microsoft.Extensions.Primitives;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSessionRunner<TResult> : IActiveSessionRunner<TResult>
    {
        public ActiveSessionRunnerState State => ActiveSessionRunnerState.Complete;

        public int Position => IActiveSessionRunner<TResult>.BEGINNING_POSITION;

        public void Abort() { }

        public ActiveSessionRunnerResult<TResult> GetAvailable(int StartPosition, Int32 Advance, String? TraceIdentifier) 
        {
            return default;
        }

        public CancellationToken CompletionToken => CancellationToken.None;

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

    }
}
