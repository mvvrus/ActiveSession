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
        
        [Fact]
        public void CreateDefaultRunnermanager()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();

            using (DefaultRunnerManager manager = new DefaultRunnerManager(null, dummy_sp.Object)) {
                Assert.NotNull(manager.RunnerCreationLock);
                Assert.NotNull(manager.RunnersCounter);
                Assert.Equal(1, manager.RunnersCounter.CurrentCount);
                Assert.True(manager.CompletionToken.CanBeCanceled);
            }
        }

        [Fact]
        public void RegisterRunner()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(null, dummy_sp.Object)) {

                manager.RegisterRunner(MakeStubAs().Object, TEST_RUNNER_NUMBER);

                Assert.Equal(2, manager.RunnersCounter.CurrentCount);
            }
        }

        [Fact]
        public void UnregisterRunner()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(null, dummy_sp.Object)) {
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);

                manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);

                Assert.Equal(1, manager.RunnersCounter.CurrentCount);
            }
        }

        [Fact]
        public void ReturnRunnerNumber()
        {
            //Nothing to test right now
        }

        [Fact]
        public void GetNewRunnerNumber()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(null, dummy_sp.Object, 0, 2)) {
                int number;
                number=manager.GetNewRunnerNumber(stub_as.Object);
                Assert.Equal(0, number);
                number=manager.GetNewRunnerNumber(stub_as.Object);
                Assert.Equal(1, number);
                Assert.Throws<InvalidOperationException>(() => manager.GetNewRunnerNumber(stub_as.Object));
            }
        }

        [Fact]
        public void WaitForRunners()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(null, dummy_sp.Object)) {
                int runner_delay = 100;
                Task<Boolean> wait_for_runners_task = new Task<Boolean>(() => manager.WaitForRunners(stub_as.Object, runner_delay));
                using (ManualResetEventSlim runner_event = new ManualResetEventSlim(false)) {
                    Task runner_task = new Task(() => { runner_event.Wait(); manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER); });
                    manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);
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
            using (DefaultRunnerManager manager = new DefaultRunnerManager(null, dummy_sp.Object)) {
                int runner_delay = 300;
                Task<Boolean> wait_for_runners_task = new Task<Boolean>(() => manager.WaitForRunners(stub_as.Object,runner_delay));
                using (CancellationTokenSource cts = new CancellationTokenSource(500)) {
                    CancellationToken ct = cts.Token;
                    Task runner_task = new Task(() => {
                        while (!ct.IsCancellationRequested)
                            Task.Delay(50).Wait();
                        manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);
                    });
                    manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);
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

        [Fact]
        public void Dispose_RunnerManager()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            DefaultRunnerManager manager = new DefaultRunnerManager(null, dummy_sp.Object, 0, 2);

            manager.Dispose();
            Assert.Throws<ObjectDisposedException>(() => manager.CompletionToken);
            Assert.Throws<ObjectDisposedException>(() => manager.RegisterRunner(MakeStubAs().Object, TEST_RUNNER_NUMBER));
        }

        Mock<IActiveSession> MakeStubAs()
        {
            Mock<IActiveSession> stub_as = new Mock<IActiveSession>();
            stub_as.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
            return stub_as;
        }

    }
}
