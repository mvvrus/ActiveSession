namespace MVVrus.AspNetCore.ActiveSession
{
    public struct ActiveSessionRunnerResult<TResult> 
    {
        public TResult Result;
        public ActiveSessionRunnerState State;
        public Int32 Position;
    }
}
