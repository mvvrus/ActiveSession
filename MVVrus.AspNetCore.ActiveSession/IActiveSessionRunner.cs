namespace MVVrus.AspNetCore.ActiveSession
{
    public interface IActiveSessionRunner<TResult>
    {
        public ActiveSessionRunnerState State { get; }
        Int32 Position { get; }
        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(Int32 StartPosition, CancellationToken token=default);
        public ActiveSessionRunnerResult<TResult> GetAvailable(Int32 StartPosition);
        public const int BEGINNING = 0;

    }
}
