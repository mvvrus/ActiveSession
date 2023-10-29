namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class RunnerManagerFactory : IRunnerManagerFactory
    {
        public IRunnerManager GetRunnerManager(
            ILogger? Logger, 
            IServiceProvider Services, 
            Int32 MinRunnerNumber = 0, 
            Int32 MaxRunnerNumber = int.MaxValue)
        {
            return new DefaultRunnerManager(Logger, Services, MinRunnerNumber, MaxRunnerNumber);
        }
    }
}
