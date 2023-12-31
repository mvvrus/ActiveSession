﻿using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Linq.Expressions;

[assembly: CollectionBehavior(MaxParallelThreads = 10)]
namespace ActiveSession.Tests
{
    public class DefaultRunnerManagerTests
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
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1));
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
            Mock<IActiveSessionRunner<Result1>> dummy_runner = new Mock<IActiveSessionRunner<Result1>>();
            using (DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object)) {
                //Test: register a runner for an unregistered session
                //Act and assess: it throws
                Assert.ThrowsAsync<InvalidOperationException>(() => manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER)!)
                    .GetAwaiter().GetResult();

                //Test: register a runner for the registered session
                //Arrange more - register session & runner
                manager.RegisterSession(stub_as.Object);
                manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1));
                //Act
                manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER)?.GetAwaiter().GetResult();
                //Assess
                Assert.Equal(1, manager.RunnersCounter.CurrentCount); //+1 for session; Fragile:depends on implementation
                Assert.Null(manager.GetRunnerInfo(stub_as.Object, TEST_RUNNER_NUMBER));
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

        //Test case: test Dispose method
        [Fact]
        public void Dispose_RunnerManager()
        {
            //Arrange
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_as = MakeStubAs();
            Mock<IActiveSessionRunner<Result1>> dummy_runner = new Mock<IActiveSessionRunner<Result1>>();
            DefaultRunnerManager manager = new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object);
            manager.RegisterSession(stub_as.Object);
            //Act
            manager.Dispose();
            //Assert that internal countdown even object is disposed

            //Test case: attemp to call RegisterRunner for disposed instance
            //Act & assess: throws
            Assert.Throws<ObjectDisposedException>(() => manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1)));

            //Test case: attemp to call UnregisterRunner for disposed instance
            //Act & assess: throws
            Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UnregisterRunner(stub_as.Object, TEST_RUNNER_NUMBER)??Task.CompletedTask);

            //Test case: double Dispose()
            //Act & assess: does not throw
            manager.Dispose();
        }

        Expression<Action<IActiveSessionRunner>> AbortExpression = s => s.Abort();
        Mock<IActiveSessionRunner> MockRunner()
        {
            Mock<IActiveSessionRunner> result = new Mock<IActiveSessionRunner>();
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
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object);
            manager.RegisterSession(stub_as.Object);
            //Test case: AbortAll with no runners
            //Act and assess: does not throw
            manager.AbortAll(stub_as.Object);

            //Test case: AbortAll with runners
            //Arrange
            Mock<IActiveSessionRunner>[] runners = new Mock<IActiveSessionRunner>[3];
            manager=new DefaultRunnerManager(
                new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME).Logger, dummy_sp.Object);
            manager.RegisterSession(stub_as.Object);
            for (int i = 0; i<runners.Length; i++) {
                runners[i]=MockRunner();
                manager.RegisterRunner(stub_as.Object, i, runners[i].Object, typeof(Object));
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
            mock_logger=new(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
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
                    mock_logger=new(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
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
                    mock_logger=new(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
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
                    mock_logger=new(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
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
                    mock_logger=new(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
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
                    mock_logger=new(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
                    manager=new DefaultRunnerManager(mock_logger.Logger, dummy_sp.Object);
                    manager.RegisterSession(stub_as.Object);
                    RegisterTestRunner(manager, stub_as.Object, new RunnerBase(cts1),
                        () => { evt_unreg1.WaitOne(); unreg_cnt1++; });
                    result=manager.PerformRunnersCleanupAsync(stub_as.Object);
                    Mock<IActiveSessionRunner<Result1>> dummy_runner = new Mock<IActiveSessionRunner<Result1>>();
                    //Act & Assess
                    Assert.Throws<InvalidOperationException>(() => manager.RegisterRunner(stub_as.Object, TEST_RUNNER_NUMBER, dummy_runner.Object, typeof(Result1)));
                    evt_unreg1.Set();
                    Assert.True(result.Wait(CLEANUP_TIMEOUT));
                }
            }

            //Test case attempt to make duplicate PerformCleanup call 
            //Arrange
            using (ManualResetEvent evt_unreg1 = new ManualResetEvent(false)) {
                int unreg_cnt1 = 0;
                using (CancellationTokenSource cts1 = new CancellationTokenSource()) {
                    mock_logger=new(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  Auxilary methods and classes
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Mock<IActiveSession> MakeStubAs()
        {
            Mock<IActiveSession> stub_as = new Mock<IActiveSession>();
            stub_as.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
            return stub_as;
        }

        int RegisterTestRunner(IRunnerManager Manager, IActiveSession SessionKey, IActiveSessionRunner Runner,
            Action? UnregCallback=null)
        {
            int number = Manager.GetNewRunnerNumber(SessionKey);
            Manager.RegisterRunner(SessionKey, number, Runner, typeof(Object));
            Runner.            CompletionToken.Register(() => Task.Run(() => UnregisterTestRunner(new UnregisterState(Manager,SessionKey,number,UnregCallback))));
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

        class RunnerBase : IActiveSessionRunner
        {
            protected CancellationTokenSource _completionTokenSource;
            
            public RunnerBase(CancellationTokenSource? Cts=null)
            {
                _completionTokenSource=Cts??new CancellationTokenSource();
            }

            public ActiveSessionRunnerState State { get; protected set; }

            public Int32 Position => 0;

            public void Abort()
            {
                State=ActiveSessionRunnerState.Aborted;
                _completionTokenSource.Cancel();
            }

            public CancellationToken CompletionToken => _completionTokenSource.Token;
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