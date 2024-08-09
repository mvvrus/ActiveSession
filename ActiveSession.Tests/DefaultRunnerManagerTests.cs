using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using LogValues = System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>;
using System.Linq.Expressions;

[assembly: CollectionBehavior(MaxParallelThreads = 16)]
namespace ActiveSession.Tests
{
    public class DefaultRunnerManagerTests
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // DefaultRunnerManager tests
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        const String TEST_SESSION_ID = "TestSessionId";
        const Int32 TEST_RUNNER_NUMBER = 0;
        const String TEST_TRACE_ID="TEST_TRACE_ID"; 

        //Test case: create DefaultRunnerManager instance        
        [Fact]
        public void CreateDefaultRunnermanager()
        {
            //Arrange
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            //Act
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME).Logger, dummy_sp.Object)) {
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
                new MockedLogger(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                //Act and assess: does not throw
                manager.RegisterSession(stub_as.Object);

                //Test case: register the same session the 2nd time (already arranged)
                //Act and assess: does not throw
                manager.RegisterSession(stub_as.Object);

                //Test case: try to register different session (already arranged)
                //Act and assess: it throws InvalidOperationException
                Assert.Throws<InvalidOperationException>(() => manager.RegisterSession(new Mock<IActiveSession>().Object));
            }
        }

        //Test group: test RegisterRunner method & GetRunnerInfo method
        [Fact]
        public void RegisterRunner()
        {
            //Arrange both tests
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IRunner<Result1>> dummy_runner = new Mock<IRunner<Result1>>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                //Test: register a runner for an unregistered session
                //Act and assess: it throws
                Assert.Throws<InvalidOperationException>(() => manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1), TEST_TRACE_ID));
                //Test: register a runner for the registered session
                //Arrange more - register session
                manager.RegisterSession(stub_as.Object);
                //Act
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1), TEST_TRACE_ID);
                //Assess
                Assert.Equal(2, manager.RunnersCounter.CurrentCount); //+1 for session; Fragile:depends on implementation
                RunnerInfo? info = manager.GetRunnerInfo(stub_as.Object, TEST_RUNNER_NUMBER);
                Assert.NotNull(info);
                Assert.Equal(dummy_runner.Object, info!.Runner);
                Assert.Equal(typeof(Result1), info!.ResultType);
                Assert.Equal(TEST_RUNNER_NUMBER, info!.Number);
            }
        }

        //Test group: test UnregisterRunner method & GetRunnerInfo method
        [Fact]
        public void UnregisterRunner()
        {
            //Arrange both tests
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IRunner<Result1>> dummy_runner;
            MockedLoggerFactory logger_factory= new MockedLoggerFactory();
            Boolean? in_time = null;
            Action<LogLevel, EventId, LogValues> log_callback = (_, _, vals) => in_time=(Boolean?)(vals[1].Value);
            MockedLogger mock_logger;

            dummy_runner = new Mock<IRunner<Result1>>();
            logger_factory.ResetAllCategories();
            mock_logger = logger_factory.MonitorLoggerCategory(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
            mock_logger.MonitorLogEntry(LogLevel.Debug, LogIds.D_MANAGERRUNNERCLEANUPWAITFINISHED, log_callback);
            in_time=null;
            using (DefaultRunnerManager manager = new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object)) {
                //Test: register a runner for an unregistered session
                //Act and assess: it throws
                Assert.ThrowsAsync<InvalidOperationException>(() => manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER)!)
                    .GetAwaiter().GetResult();

                //Test: register a runner for the registered session
                //Arrange more - register session & runner
                manager.RegisterSession(stub_as.Object);
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1), TEST_TRACE_ID);
                //Act
                manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER)?.GetAwaiter().GetResult();
                //Assess
                Assert.Equal(1, manager.RunnersCounter.CurrentCount); //+1 for session; Fragile:depends on implementation
                Assert.Null(manager.GetRunnerInfo(stub_as.Object, TEST_RUNNER_NUMBER));
                //Test case: cleanup of the runner after unregister if no timeout set
                Assert.Equal(true, in_time);
            }
            Int32 cleanup_timeout = 2000;
            Mock<IDisposable> runner_disposable;
            Task? unreg_task; 
            //Test case: cleanup of the runner after unregister has been completed within timeout
            //Arrrange
            dummy_runner = new Mock<IRunner<Result1>>();
            runner_disposable= dummy_runner.As<IDisposable>();
            runner_disposable.Setup(s => s.Dispose()).Callback(()=>Thread.Sleep(cleanup_timeout/2));
            logger_factory.ResetAllCategories();
            mock_logger = logger_factory.MonitorLoggerCategory(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
            mock_logger.MonitorLogEntry(LogLevel.Debug, LogIds.D_MANAGERRUNNERCLEANUPWAITFINISHED, log_callback);
            in_time=null;
            using(DefaultRunnerManager manager = new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object, cleanup_timeout)) {
                manager.RegisterSession(stub_as.Object);
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1), TEST_TRACE_ID);
                //Act
                unreg_task=manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);
                //Assess
                manager._cleanupLoggingTask?.GetAwaiter().GetResult();
                Assert.Equal(true, in_time);
                unreg_task?.GetAwaiter().GetResult();
            }
            //Test case: cleanup of the runner after unregister hasn't been completed within timeout
            dummy_runner = new Mock<IRunner<Result1>>();
            runner_disposable= dummy_runner.As<IDisposable>();
            runner_disposable.Setup(s => s.Dispose()).Callback(() => Thread.Sleep(cleanup_timeout*3/2));
            logger_factory.ResetAllCategories();
            mock_logger = logger_factory.MonitorLoggerCategory(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
            mock_logger.MonitorLogEntry(LogLevel.Debug, LogIds.D_MANAGERRUNNERCLEANUPWAITFINISHED, log_callback);
            in_time=null;
            using(DefaultRunnerManager manager = new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object, cleanup_timeout)) {
                manager.RegisterSession(stub_as.Object);
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1), TEST_TRACE_ID);
                //Act
                unreg_task=manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER);
                //Assess
                manager._cleanupLoggingTask?.GetAwaiter().GetResult();
                Assert.Equal(false, in_time);
                unreg_task?.GetAwaiter().GetResult();
            }

        }

        //Test group: test ReturnRunnerNumber method (no runner number reuse is implemented yet)
        [Fact]
        public void ReturnRunnerNumber()
        {
            //Arrange both tests
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IRunner> dummy_runner = new Mock<IRunner>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME).Logger, dummy_sp.Object)) {
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
                new MockedLogger(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME).Logger, dummy_sp.Object, null, 0, 2)) {
                int number;
                //Test: try to get a new runner number for an unregistered session
                //Act and assess:throws
                Assert.Throws<InvalidOperationException>(() => manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID));

                //Test: get a new runner number for the registered session
                //Arrange more - register session
                manager.RegisterSession(stub_as.Object);
                //Act
                number=manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID);
                //Assess
                Assert.Equal(0, number);

                //Test: get a new runner number for the registered session ones more
                //Act
                number=manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID);
                //Assess
                Assert.Equal(1, number);
                //Test: try to get a new runner number when the numbers are exhausted
                //Act & assess: it throws
                Assert.Throws<InvalidOperationException>(() => manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID));
            }
        }

        //Test case: test Dispose method
        [Fact]
        public void Dispose_RunnerManager()
        {
            //Arrange
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IRunner<Result1>> dummy_runner = new Mock<IRunner<Result1>>();
            DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME).Logger, dummy_sp.Object);
            manager.RegisterSession(stub_as.Object);
            //Act
            manager.Dispose();

            //Test case: attemp to call RegisterRunner for disposed instance
            //Act & assess: throws
            Assert.Throws<ObjectDisposedException>(() => manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1), TEST_TRACE_ID));

            //Test case: attemp to call UnregisterRunner for disposed instance
            //Act & assess: throws
            Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER)??Task.CompletedTask);

            //Test case: double Dispose()
            //Act & assess: does not throw
            manager.Dispose();
        }

        Expression<Action<IRunner>> AbortExpression = s => s.Abort(null);
        Mock<IRunner> MockRunner()
        {
            Mock<IRunner> result = new Mock<IRunner>();
            result.Setup(AbortExpression);
            return result;
        }

        //Test group: test AbortAll
        [Fact]
        public void AbortAll()
        {
            //Arrange
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME).Logger, dummy_sp.Object);
            manager.RegisterSession(stub_as.Object);
            //Test case: AbortAll with no runners
            //Act and assess: does not throw
            manager.AbortAll(stub_as.Object);

            //Test case: AbortAll with runners
            //Arrange
            Mock<IRunner>[] runners = new Mock<IRunner>[3];
            manager=new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME).Logger, dummy_sp.Object);
            manager.RegisterSession(stub_as.Object);
            for (int i = 0; i<runners.Length; i++) {
                runners[i]=MockRunner();
                manager.RegisterRunner(stub_as.Object, i, runners[i].Object, typeof(Object), TEST_TRACE_ID);
            }
            //Act
            manager.AbortAll(stub_as.Object);
            //Assess
            for (int i = 0; i<runners.Length; i++) runners[i].Verify(AbortExpression, Times.Once);
        }


        //Test group: PerformRunnersCleanup
        [Fact]
        public void PerformRunnersCleanup()
        {
            const Int32 CLEANUP_TIMEOUT= 20000;
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            MockedLogger mock_logger;
            DefaultRunnerManager manager;
            Task result;

            //Test case: cleanup wrong session
            //Arrange
            mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
            manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object);
            //Act & Assess
            Assert.ThrowsAsync<InvalidOperationException>(() => manager.PerformRunnersCleanupAsync(stub_as.Object))
                        .GetAwaiter().GetResult();

            //Test case: cleanup no runners
            //Arrange more
            manager.RegisterSession(stub_as.Object);
            //Act
            result=manager.PerformRunnersCleanupAsync(stub_as.Object);
            //Assess
            Assert.Equal(TaskStatus.RanToCompletion, result.Status);
            Assert.True(manager.IsDisposed());

            //Test case: cleanup a non-disposable runner 
            //Arrange
            using (ManualResetEvent evt_unreg1=new ManualResetEvent(false)) {
                int unreg_cnt1 = 0;
                using(CancellationTokenSource cts1=new CancellationTokenSource()) {
                    mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
                    manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object);
                    manager.RegisterSession(stub_as.Object);
                    RegisterTestRunner(manager, stub_as.Object, new RunnerBase(cts1), 
                        () => { evt_unreg1.WaitOne(); unreg_cnt1++; });
                    //Act
                    result=manager.PerformRunnersCleanupAsync(stub_as.Object);
                    //Assess
                    Assert.Equal(TaskStatus.WaitingForActivation, result.Status);
                    evt_unreg1.Set();
                    Assert.True(result.Wait(CLEANUP_TIMEOUT));
                    Assert.Equal(1, unreg_cnt1);
                    Assert.Equal(TaskStatus.RanToCompletion, result.Status);
                    Assert.True(manager.IsDisposed());
                }
            }

            //Test case: cleanup a disposable runner 
            //Arrange
            using (ManualResetEvent evt_unreg2 = new ManualResetEvent(false)) {
                int unreg_cnt2 = 0;
                using (CancellationTokenSource cts2 = new CancellationTokenSource()) {
                    mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
                    manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object);
                    manager.RegisterSession(stub_as.Object);
                    Boolean disp2 = false;
                    using(ManualResetEvent evt_disp2=new ManualResetEvent(false)) {
                        RegisterTestRunner(manager, stub_as.Object,
                            new RunnerDisposable(cts2, () => { evt_disp2.WaitOne(); disp2=true; }),
                            () => { evt_unreg2.WaitOne(); unreg_cnt2++; });
                        //Act
                        result=manager.PerformRunnersCleanupAsync(stub_as.Object);
                        //Assess
                        Assert.Equal(TaskStatus.WaitingForActivation, result.Status);
                        evt_unreg2.Set();
                        Assert.Equal(TaskStatus.WaitingForActivation, result.Status);
                        evt_disp2.Set();
                        Assert.True(result.Wait(CLEANUP_TIMEOUT));
                        Assert.Equal(1, unreg_cnt2);
                        Assert.True(disp2);
                        Assert.Equal(TaskStatus.RanToCompletion, result.Status);
                        Assert.True(manager.IsDisposed());
                    }
                }
            }

            //Test case: cleanup an asynchronously disposable runner 
            //Arrange
            using (ManualResetEvent evt_unreg3 = new ManualResetEvent(false)) {
                int unreg_cnt3 = 0;
                using (CancellationTokenSource cts3 = new CancellationTokenSource()) {
                    mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
                    manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object);
                    manager.RegisterSession(stub_as.Object);
                    Boolean disp3 = false;
                    using (ManualResetEvent evt_disp3 = new ManualResetEvent(false)) {
                        RegisterTestRunner(manager, stub_as.Object,
                            new RunnerAsyncDisposable(cts3, () => { evt_disp3.WaitOne(); disp3=true; }),
                            () => { evt_unreg3.WaitOne(); unreg_cnt3++; });
                        //Act
                        result=manager.PerformRunnersCleanupAsync(stub_as.Object);
                        //Assess
                        Assert.Equal(TaskStatus.WaitingForActivation, result.Status);
                        evt_unreg3.Set();
                        Assert.Equal(TaskStatus.WaitingForActivation, result.Status);
                        evt_disp3.Set();
                        Assert.True(result.Wait(CLEANUP_TIMEOUT));
                        Assert.Equal(1, unreg_cnt3);
                        Assert.True(disp3);
                        Assert.Equal(TaskStatus.RanToCompletion, result.Status);
                        Assert.True(manager.IsDisposed());
                    }
                }
            }

            //Test case: cleanup all three above runners
            //Arrange
            using(ManualResetEvent evt_unreg1 = new ManualResetEvent(false)) {
            using(ManualResetEvent evt_unreg2 = new ManualResetEvent(false)) {
            using(ManualResetEvent evt_unreg3 = new ManualResetEvent(false)) {
                int unreg_cnt1 = 0, unreg_cnt2 = 0,unreg_cnt3 = 0;
                using(CancellationTokenSource cts1=new CancellationTokenSource()) {
                using(CancellationTokenSource cts2 = new CancellationTokenSource()) {
                using (CancellationTokenSource cts3 = new CancellationTokenSource()) {
                    mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
                    manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object);
                    manager.RegisterSession(stub_as.Object);
                    Boolean disp2 = false, disp3 = false;
                    using(ManualResetEvent evt_disp2=new ManualResetEvent(false)) {
                    using(ManualResetEvent evt_disp3=new ManualResetEvent(false)) {
                        RegisterTestRunner(manager, stub_as.Object, new RunnerBase(cts1), 
                            () => { evt_unreg1.WaitOne(); unreg_cnt1++; });
                        RegisterTestRunner(manager, stub_as.Object,
                            new RunnerDisposable(cts2, () => { evt_disp2.WaitOne(); disp2=true; }),
                            () => { evt_unreg2.WaitOne(); unreg_cnt2++; });
                        RegisterTestRunner(manager, stub_as.Object,
                            new RunnerAsyncDisposable(cts3, () => { evt_disp3.WaitOne(); disp3=true; }),
                            () => { evt_unreg3.WaitOne(); unreg_cnt3++; });
                        //Act
                        result=manager.PerformRunnersCleanupAsync(stub_as.Object);
                        //Assess
                        Assert.Equal(TaskStatus.WaitingForActivation, result.Status);
                        evt_unreg1.Set();
                        evt_unreg2.Set();
                        evt_unreg3.Set();
                        Assert.Equal(TaskStatus.WaitingForActivation, result.Status);
                        evt_disp2.Set();
                        evt_disp3.Set();
                        Assert.True(result.Wait(CLEANUP_TIMEOUT));
                        Assert.Equal(1, unreg_cnt1);
                        Assert.Equal(1, unreg_cnt2);
                        Assert.Equal(1, unreg_cnt3);
                        Assert.True(disp2);
                        Assert.True(disp3);
                        Assert.Equal(TaskStatus.RanToCompletion, result.Status);
                        Assert.True(manager.IsDisposed());
                    }}
                }}}
            }}}

            //Test case: attempt to register a runner after cleanup initiation (should throw InvalidOperationException)
            //Arrange
            using (ManualResetEvent evt_unreg1 = new ManualResetEvent(false)) {
                int unreg_cnt1 = 0;
                using (CancellationTokenSource cts1 = new CancellationTokenSource()) {
                    mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
                    manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object);
                    manager.RegisterSession(stub_as.Object);
                    RegisterTestRunner(manager, stub_as.Object, new RunnerBase(cts1),
                        () => { evt_unreg1.WaitOne(); unreg_cnt1++; });
                    result=manager.PerformRunnersCleanupAsync(stub_as.Object);
                    Mock<IRunner<Result1>> dummy_runner = new Mock<IRunner<Result1>>();
                    //Act & Assess
                    Assert.Throws<InvalidOperationException>(() => manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1), TEST_TRACE_ID));
                    evt_unreg1.Set();
                    Assert.True(result.Wait(CLEANUP_TIMEOUT));
                }
            }

            //Test case attempt to make duplicate PerformCleanup call 
            //Arrange
            using (ManualResetEvent evt_unreg1 = new ManualResetEvent(false)) {
                int unreg_cnt1 = 0;
                using (CancellationTokenSource cts1 = new CancellationTokenSource()) {
                    mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
                    manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object);
                    manager.RegisterSession(stub_as.Object);
                    RegisterTestRunner(manager, stub_as.Object, new RunnerBase(cts1),
                        () => { evt_unreg1.WaitOne(); unreg_cnt1++; });
                    result=manager.PerformRunnersCleanupAsync(stub_as.Object);
                    //Act 
                    Task result2= manager.PerformRunnersCleanupAsync(stub_as.Object);
                    //Assess
                    Assert.Equal(TaskStatus.RanToCompletion, result2.Status);
                    evt_unreg1.Set();
                    Assert.True(result.Wait(CLEANUP_TIMEOUT));

                    //Test case attempt to call PerformCleanup after disposal
                    Assert.ThrowsAsync<ObjectDisposedException>(()=>manager.PerformRunnersCleanupAsync(stub_as.Object))
                        .GetAwaiter().GetResult();
                }
            }
        }

        //Test group: tasks returned by GetRunnerCleanupTrackingTask
        [Fact]
        public void GetRunnerCleanupTrackingTask()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            MockedLogger mock_logger;
            DefaultRunnerManager manager;
            Task? task, task2;
            int number, number2;

            mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
            using(manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object)) {
                manager.RegisterSession(stub_as.Object);
                //Test case: non-existing runner
                number = manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID);
                task=manager.GetRunnerCleanupTrackingTask(stub_as.Object, number);
                Assert.Null(task);
                //Test case: existing runner, first request
                manager.RegisterRunner(stub_as.Object, number, new RunnerBase(), typeof(Object), TEST_TRACE_ID);
                task=manager.GetRunnerCleanupTrackingTask(stub_as.Object, number);
                Assert.NotNull(task);
                //Test case: existing runner, non-first request
                task2=manager.GetRunnerCleanupTrackingTask(stub_as.Object, number);
                Assert.Same(task,task2);
                //Test case: request for additional existing runner
                number2 = manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID);
                manager.RegisterRunner(stub_as.Object, number2, new RunnerBase(), typeof(Object), TEST_TRACE_ID);
                task2=manager.GetRunnerCleanupTrackingTask(stub_as.Object, number2);
                Assert.NotNull(task2);
                Assert.NotSame(task, task2);
            }
        }

        //Test group: cleanup tracking tasks (from both UnregisterRunner and GetRunnerCleanupTrackingTask) completion
        [Fact]
        public void CleanupTrackingTasksCompletion()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            ManualResetEventSlim pause_event;
            MockedLogger mock_logger;
            DefaultRunnerManager manager;
            int number;
            Task? unreg_task;
            Task? tracking_task;
            const Int32 TIMEOUT = 5000;

            mock_logger=new(ActiveSessionConstants.RUNNERMANAGER_CATEGORY_NAME);
            using(manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object)) {
                using(pause_event=new ManualResetEventSlim()) {
                    manager.RegisterSession(stub_as.Object);
                    //Test case: non-disposable runner
                    number = manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID);
                    manager.RegisterRunner(stub_as.Object, number, new RunnerBase(), typeof(Object), TEST_TRACE_ID);
                    tracking_task=manager.GetRunnerCleanupTrackingTask(stub_as.Object, number);
                    Assert.NotNull(tracking_task);
                    unreg_task=manager.UnregisterRunner(stub_as.Object,number);
                    Assert.Null(unreg_task);
                    Assert.True(tracking_task.IsCompletedSuccessfully);
                    //Test case: disposable runner
                    pause_event.Reset();
                    number = manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID);
                    manager.RegisterRunner(stub_as.Object, number, new RunnerDisposable(Callback: () => pause_event.Wait()), typeof(Object), TEST_TRACE_ID);
                    tracking_task=manager.GetRunnerCleanupTrackingTask(stub_as.Object, number);
                    Assert.NotNull(tracking_task);
                    unreg_task=manager.UnregisterRunner(stub_as.Object, number);
                    Assert.NotNull(unreg_task);
                    Thread.Sleep(50);
                    Assert.False(tracking_task.IsCompleted);
                    Assert.False(unreg_task.IsCompleted);
                    pause_event.Set();
                    Assert.True(unreg_task.Wait(TIMEOUT));
                    Assert.True(tracking_task.Wait(TIMEOUT));
                    //Test case: async-disposable runner 
                    pause_event.Reset();
                    number = manager.GetNewRunnerNumber(stub_as.Object, TEST_TRACE_ID);
                    manager.RegisterRunner(stub_as.Object, number, new RunnerAsyncDisposable(Callback: () => pause_event.Wait()), typeof(Object), TEST_TRACE_ID);
                    tracking_task=manager.GetRunnerCleanupTrackingTask(stub_as.Object, number);
                    Assert.NotNull(tracking_task);
                    unreg_task=manager.UnregisterRunner(stub_as.Object, number);
                    Assert.NotNull(unreg_task);
                    Thread.Sleep(50);
                    Assert.False(tracking_task.IsCompleted);
                    Assert.False(unreg_task.IsCompleted);
                    pause_event.Set();
                    Assert.True(unreg_task.Wait(TIMEOUT));
                    Assert.True(tracking_task.Wait(TIMEOUT));
                }
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  Auxilary methods and classes
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Mock<IActiveSession> MakeStubAs()
        {
            Mock<IActiveSession> stub_as = new Mock<IActiveSession>();
            stub_as.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
            return stub_as;
        }

        int RegisterTestRunner(IRunnerManager Manager, IActiveSession SessionKey, IRunner Runner,
            Action? UnregCallback=null)
        {
            int number = Manager.GetNewRunnerNumber(SessionKey, TEST_TRACE_ID);
            Manager.RegisterRunner(SessionKey, number, Runner, typeof(Object), TEST_TRACE_ID);
            Runner.CompletionToken.Register(() => Task.Run(() => UnregisterTestRunner(new UnregisterState(Manager,SessionKey,number,UnregCallback))));
            //Runner.GetCompletionToken().Register(UnregisterTestRunner,new UnregisterState(Manager, SessionKey, number, UnregCallback));
            return number;
        }

        record UnregisterState(IRunnerManager Manager, IActiveSession SessionKey, int RunnerNumber, Action? Callback);

        static void UnregisterTestRunner(Object? State)
        {
            UnregisterState state = (UnregisterState)State!;
            state.Callback?.Invoke();
            state.Manager.UnregisterRunner(state.SessionKey, state.RunnerNumber);
            state.Manager.ReturnRunnerNumber(state.SessionKey, state.RunnerNumber);
        }

        class RunnerBase : IRunner
        {
            protected CancellationTokenSource _completionTokenSource;
            
            public RunnerBase(CancellationTokenSource? Cts=null)
            {
                _completionTokenSource=Cts??new CancellationTokenSource();
            }

            public RunnerStatus Status { get; protected set; }

            public Int32 Position => 0;

            public RunnerStatus Abort(String? TraceIdentifier = null)
            {
                Status=RunnerStatus.Aborted;
                _completionTokenSource.Cancel();
                return Status;
            }

            public RunnerBkgProgress GetProgress()
            {
                throw new NotImplementedException();
            }

            public CancellationToken CompletionToken => _completionTokenSource.Token;

            public Exception? Exception => throw new NotImplementedException();

            public Boolean IsBackgroundExecutionCompleted => throw new NotImplementedException();

            public Object? ExtraData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }

        class RunnerDisposable: RunnerBase, IDisposable
        {
            Action? _callback;

            public RunnerDisposable(CancellationTokenSource? Cts = null, Action? Callback=null) : base(Cts) 
            {
                _callback=Callback;
            }

            public void Dispose()
            {
                _callback?.Invoke();
            }
        }

        class RunnerAsyncDisposable : RunnerBase, IDisposable, IAsyncDisposable
        {
            Action? _callback;

            public RunnerAsyncDisposable(CancellationTokenSource? Cts = null, Action? Callback = null) : base(Cts)
            {
                _callback=Callback;
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public async ValueTask DisposeAsync()
            {
                if (_callback!=null) {
                    await   Task.Run(()=>_callback!.Invoke());
                }
            }
        }

    }
}
