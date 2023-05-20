using Microsoft.Extensions.Primitives;

namespace MVVrus.AspNetCore.ActiveSession
{
    public interface IActiveSessionRunner
    {
        public ActiveSessionRunnerState State { get; }
        Int32 Position { get; }
        public void Abort();
        IChangeToken GetCompletionToken();
    }

    public interface IActiveSessionRunner<TResult>:IActiveSessionRunner
    {
        public const int BEGINNING = 0;

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(Int32 StartPosition, CancellationToken token=default);
        public ActiveSessionRunnerResult<TResult> GetAvailable(Int32 StartPosition);
    }
}
