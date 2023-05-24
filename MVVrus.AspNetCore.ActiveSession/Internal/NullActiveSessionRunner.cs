﻿using Microsoft.Extensions.Primitives;

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

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(int StartPosition, CancellationToken token = default)
        {
            return new ValueTask<ActiveSessionRunnerResult<TResult>>(Task.FromResult<ActiveSessionRunnerResult<TResult>>(default));
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

        static IChangeToken s_NullChangeToken = new NullChangeToken();
    }
}
