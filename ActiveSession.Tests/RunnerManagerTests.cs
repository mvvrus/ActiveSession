using MVVrus.AspNetCore.ActiveSession.Internal;

namespace ActiveSession.Tests
{
    public class RunnerManagerTests
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // DefaultRunnerManager tests
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        const String TEST_SESSION_ID = "TestSessionId";
        const Int32 TEST_RUNNER_NUMBER = 0;

        //Test case: create DefaultRunnerManager instance        
        [Fact]
        public void CreateDefaultRunnermanager()
        {
            //Arrange
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            //Act
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                //Assess
                Assert.NotNull(manager.RunnerCreationLock);
                Assert.NotNull(manager.RunnersCounter);
                Assert.Equal(1, manager.RunnersCounter.CurrentCount);
            }
        }

        //Test group: test RegisterSession method
        [Fact]
        public void RegisterSession()
        {
            //Test case: register session the 1st time
            //Arrange
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                //Act and assess: does not throw
                manager.RegisterSession(stub_as.Object);
                //Test case: register the same session the 2nd time (already arranged)
                //Act and assess: does not throw
                manager.RegisterSession(stub_as.Object);
                //Test case: try to register different session (already arranged)
                //Act and assess: it throws InvalidOperationException
                Assert.Throws<InvalidOperationException>(()=>manager.RegisterSession(new Mock<IActiveSession>().Object));
            }
        }

        //Test group: test RegisterRunner method
        [Fact]
        public void RegisterRunner()
        {
            //Arrange both tests
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IActiveSessionRunner<Result1>> dummy_runner = new Mock<IActiveSessionRunner<Result1>>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                //Test: register a runner for an unregistered session
                //Act and assess: it throws
                Assert.Throws<InvalidOperationException>(() => manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1)));
                //Test: register a runner for the registered session
                //Arrange more - register session
                manager.RegisterSession(stub_as.Object);
                //Act
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER,dummy_runner.Object, typeof(Result1));
                //Assess
                Assert.Equal(2, manager.RunnersCounter.CurrentCount);
            }
        }

        //Test group: test UnregisterRunner method
        [Fact]
        public void UnregisterRunner()
        {
            //Arrange both tests
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IActiveSessionRunner<Result1>> dummy_runner = new Mock<IActiveSessionRunner<Result1>>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                //Test: register a runner for an unregistered session
                //Act and assess: it throws
                Assert.Throws<InvalidOperationException>(() => manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER));

                //Test: register a runner for the registered session
                //Arrange more - register session & runner
                manager.RegisterSession(stub_as.Object);
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER,dummy_runner.Object, typeof(Result1));
                //Act
                manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);
                //Assess
                Assert.Equal(1, manager.RunnersCounter.CurrentCount);
            }
        }

        //Test group: test ReturnRunnerNumber method (no runner number reuse is implemented yet)
        [Fact]
        public void ReturnRunnerNumber()
        {
            //Arrange both tests
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IActiveSessionRunner> dummy_runner = new Mock<IActiveSessionRunner>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                //Test: return an unused runner number for an unregistered session
                //Act and assess:it throws
                Assert.Throws<InvalidOperationException>(() => manager.ReturnRunnerNumber(stub_as.Object, TEST_RUNNER_NUMBER));

                //Test: return an unused runner number for the registered session
                //Arrange more - register session
                manager.RegisterSession(stub_as.Object);
                //Act and assess (it doesn't throw)
                manager.ReturnRunnerNumber(stub_as.Object, TEST_RUNNER_NUMBER);
            }
        }

        //Test group: test GetNewRunnerNumber method (no runner number reuse is implemented yet)
        [Fact]
        public void GetNewRunnerNumber()
        {
            //Arrange stuff common ti all tests
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object, 0, 2)) {
                int number;
                //Test: try to get a new runner number for an unregistered session
                //Act and assess:throws
                Assert.Throws<InvalidOperationException>(() => manager.GetNewRunnerNumber(stub_as.Object));

                //Test: get a new runner number for the registered session
                //Arrange more - register session
                manager.RegisterSession(stub_as.Object);
                //Act
                number=manager.GetNewRunnerNumber(stub_as.Object);
                //Assess
                Assert.Equal(0, number);

                //Test: get a new runner number for the registered session ones more
                //Act
                number=manager.GetNewRunnerNumber(stub_as.Object);
                //Assess
                Assert.Equal(1, number);
                //Test: try to get a new runner number when the numbers are exhausted
                //Act & assess: it throws
                Assert.Throws<InvalidOperationException>(() => manager.GetNewRunnerNumber(stub_as.Object));
            }
        }

        [Fact]
        public void WaitForRunners()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IActiveSessionRunner<Result1>> dummy_runner = new Mock<IActiveSessionRunner<Result1>>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                int runner_delay = 100;
                Task<Boolean> wait_for_runners_task = new Task<Boolean>(() => manager.WaitForRunners(stub_as.Object, runner_delay));
                using (ManualResetEventSlim runner_event = new ManualResetEventSlim(false)) {
                    Task runner_task = new Task(() => { runner_event.Wait(); manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER); });
                    manager.RegisterSession(stub_as.Object);
                    manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER ,dummy_runner.Object, typeof(Result1));
                    runner_task.Start();
                    wait_for_runners_task.Start();
                    TaskStatus wait_task_status;
                    do {
                        Thread.Sleep(0);
                        wait_task_status=wait_for_runners_task.Status;
                    } while (wait_task_status==TaskStatus.Created||wait_task_status==TaskStatus.Running);
                    Assert.Equal(TaskStatus.WaitingToRun, wait_for_runners_task.Status);
                    runner_event.Set();
                    Assert.True(wait_for_runners_task.GetAwaiter().GetResult());
                }
            }
        }

        [Fact]
        public void WaitForRunners_Hanged()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IActiveSessionRunner<Result1>> dummy_runner = new Mock<IActiveSessionRunner<Result1>>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                int runner_delay = 300;
                Task<Boolean> wait_for_runners_task = new Task<Boolean>(() => manager.WaitForRunners(stub_as.Object,runner_delay));
                using (CancellationTokenSource cts = new CancellationTokenSource(500)) {
                    CancellationToken ct = cts.Token;
                    Task runner_task = new Task(() => {
                        while (!ct.IsCancellationRequested)
                            Task.Delay(50).Wait();
                        manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);
                    });
                    manager.RegisterSession(stub_as.Object);
                    manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1));
                    runner_task.Start();
                    wait_for_runners_task.Start();
                    TaskStatus wait_task_status;
                    do {
                        Thread.Sleep(0);
                        wait_task_status=wait_for_runners_task.Status;
                    } while (wait_task_status==TaskStatus.Created||wait_task_status==TaskStatus.Running);
                    Assert.False(wait_for_runners_task.GetAwaiter().GetResult());
                    cts.Cancel();
                    runner_task.Wait();
                }
            }
        }

        //Test case: test Dispose method
        [Fact]
        public void Dispose_RunnerManager()
        {
            //Arrange
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IActiveSessionRunner<Result1>> dummy_runner = new Mock<IActiveSessionRunner<Result1>>();
            DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object, 0, 2);
            manager.RegisterSession(stub_as.Object);
            //Act
            manager.Dispose();
            //Assert that internal countdown even object is disposed
            Assert.Throws<ObjectDisposedException>(() => manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1)));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Mock<IActiveSession> MakeStubAs()
        {
            Mock<IActiveSession> stub_as = new Mock<IActiveSession>();
            stub_as.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
            return stub_as;
        }

    }
}
