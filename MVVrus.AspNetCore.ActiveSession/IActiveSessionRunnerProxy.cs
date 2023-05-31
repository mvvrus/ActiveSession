namespace MVVrus.AspNetCore.ActiveSession
{
    public interface IActiveSessionRunnerProxy<TResult>:IActiveSessionRunner<TResult>
    {
        public Boolean IsLocal { get; }
        public ValueTask<ActiveSessionRunnerState> GetStateAsync();
        public ValueTask<Int32> GetPositionAsync();
        public ValueTask<ActiveSessionRunnerResult<TResult>> GetAvailableAsync(Int32 StartPosition);
    }
}
