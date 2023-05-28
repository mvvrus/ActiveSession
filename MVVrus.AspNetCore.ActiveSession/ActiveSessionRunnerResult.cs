namespace MVVrus.AspNetCore.ActiveSession
{
    public record struct ActiveSessionRunnerResult<TResult>(
        TResult Result, 
        ActiveSessionRunnerState State, 
        Int32 Position
     ); 
}
