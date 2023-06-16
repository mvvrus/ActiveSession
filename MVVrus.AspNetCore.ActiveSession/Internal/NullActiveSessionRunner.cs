using Microsoft.Extensions.Primitives;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSessionRunner<TResult> : IActiveSessionRunner<TResult>
    {
        public ActiveSessionRunnerState State => ActiveSessionRunnerState.Complete;

        public int Position => IActiveSessionRunner<TResult>.BEGINNING;

        public void Abort() { }

        public ActiveSessionRunnerResult<TResult> GetAvailable(int StartPosition) 
        {
            return default;
        }

        public IChangeToken GetCompletionToken()
        {
            return s_NullChangeToken;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(int StartPosition, CancellationToken token = default)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            return new ValueTask<ActiveSessionRunnerResult<TResult>>(Task.FromResult<ActiveSessionRunnerResult<TResult>>(default));
        }

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync(Int32 StartPosition, Int32 Advance, CancellationToken token = default)
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
