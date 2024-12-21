using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using RunnerCommonBase = MVVrus.AspNetCore.ActiveSession.EnumerableRunnerBase<System.Int32>;


namespace ActiveSession.Tests
{
    internal abstract class TestEnumerableSetupBase
    {
        public Int32? EnumAheadLimit { get; set; } = null;

        protected TestSequence _testSequence { get; init; }
        readonly MockedLoggerFactory _loggerFactoryMock = new MockedLoggerFactory();

        protected TestEnumerableSetupBase(Type ClassForLogger)
        {
            _loggerFactoryMock.MonitorLoggerCategory(Utilities.MakeClassCategoryName(ClassForLogger));
            _testSequence = new TestSequence();
        }

        public ILoggerFactory LoggerFactory { get => _loggerFactoryMock.LoggerFactory; }

        public Task ResumeEnumeration(Int32 Offset, TestSequence.StopAction Action = TestSequence.StopAction.Wait)
        {
            return _testSequence.Resume(Offset, Action);
        }
        
        protected abstract RunnerCommonBase CreateRunnerImpl();

        public RunnerCommonBase CreateRunner()
        {
            return CreateRunnerImpl();
        }

        public void ReleaseEnumerable()
        {
            _testSequence.ReleaseEnumerable();
        }

    }

}
