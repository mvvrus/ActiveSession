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
        public const Int32 BEGINNING = 0;
        public const Int32 DEFAULT_ADVANCE=0;

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(Int32 StartPosition, Int32 Advance, CancellationToken token=default);
        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(Int32 StartPosition, CancellationToken token = default)
        {
            return GetMore(StartPosition, DEFAULT_ADVANCE, token);
        }

        public ActiveSessionRunnerResult<TResult> GetAvailable(Int32 StartPosition);
    }
}
