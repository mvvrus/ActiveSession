namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class RunnerManagerFactory : IRunnerManagerFactory
    {
        public IRunnerManager GetRunnerManager(
            String SessionId, 
            ILogger? Logger, 
            IServiceProvider Services, 
            Int32 MinRunnerNumber = 0, 
            Int32 MaxRunnerNumber = int.MaxValue)
        {
            return new DefaultRunnerManager(SessionId, Logger, Services, MinRunnerNumber, MaxRunnerNumber);
        }
    }
}
