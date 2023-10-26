namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IRunnerManagerFactory
    {
        IRunnerManager GetRunnerManager(String sessionId
                , ILogger? logger
                , IServiceProvider Services
                , Int32 MinRunnerNumber = 0
                , Int32 MaxRunnerNumber = Int32.MaxValue);
    }
}
