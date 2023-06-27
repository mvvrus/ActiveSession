using Microsoft.Extensions.Primitives;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSessionRunner<TResult> : IActiveSessionRunner<TResult>
    {
        public ActiveSessionRunnerState State => ActiveSessionRunnerState.Complete;

        public int Position => IActiveSessionRunner<TResult>.BEGINNING_POSITION;

        public void Abort() { }

        public ActiveSessionRunnerResult<TResult> GetAvailable(int StartPosition, String? TraceIdentifier) 
        {
            return default;
        }

        public IChangeToken GetCompletionToken()
        {
            return s_NullChangeToken;
        }

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public class NullChangeToken : IChangeToken
        {
            public Boolean ActiveChangeCallbacks => false;

            public Boolean HasChanged => true;

            public IDisposable RegisterChangeCallback(Action<Object> callback, Object state)
            {
                throw new NotImplementedException();
            }
        }

        static readonly IChangeToken s_NullChangeToken = new NullChangeToken();
    }
}
