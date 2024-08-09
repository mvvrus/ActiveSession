using Microsoft.Extensions.Options;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class RunnerManagerFactory : IRunnerManagerFactory
    {
        readonly int? _cleanupLoggingTimeoutMs;

        public RunnerManagerFactory(IOptions<ActiveSessionOptions> Options)
        {
            _cleanupLoggingTimeoutMs=Options.Value.CleanupLoggingTimeoutMs;
        }

        public IRunnerManager GetRunnerManager(
            ILogger? Logger, 
            IServiceProvider Services, 
            Int32 MinRunnerNumber = 0, 
            Int32 MaxRunnerNumber = int.MaxValue)
        {
            return new DefaultRunnerManager(Logger, Services, _cleanupLoggingTimeoutMs, MinRunnerNumber, MaxRunnerNumber);
        }
    }
}
