using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MVVrus.AspNetCore.ActiveSession;
using MVVrus.AspNetCore.ActiveSession.Internal;
using Active_Session= MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession;

namespace ActiveSession.Tests
{
    public class ActiveSessionTest
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // IActiveSession tests
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        [Fact]
        public void CreateActiveSession()
        {
            ConstructorTestSetup test_setup;
            Active_Session active_session;
            test_setup=new ConstructorTestSetup();

            active_session=new Active_Session(test_setup.MockServiceScope.Object,
                test_setup.FakeStore.Object,
                test_setup.StubSession.Object,
                null);

            Assert.True(active_session.IsAvailable);
            Assert.Equal(ConstructorTestSetup.TEST_SESSION_ID, active_session.Id);
            Assert.Equal(test_setup.StubServiceProvider.Object, active_session.SessionServices);
            Assert.True(active_session.IsFresh);
            Assert.IsType<Active_Session.DefaultRunnerManager>(active_session.RunnerManager);
            Assert.True(active_session.IsDefaultRunnerManagerUsed);
            Assert.False(active_session.Disposed);

            Mock<IRunnerManager> stub_runner_manager = new Mock<IRunnerManager>();
            using (CancellationTokenSource cts = new CancellationTokenSource()) {
                stub_runner_manager.SetupGet(s => s.SessionCompletionToken).Returns(cts.Token);
                test_setup=new ConstructorTestSetup(stub_runner_manager.Object);
                active_session=new Active_Session(test_setup.MockServiceScope.Object,
                    test_setup.FakeStore.Object,
                    test_setup.StubSession.Object,
                    null);
                Assert.Equal(stub_runner_manager.Object, active_session.RunnerManager);
                Assert.Equal(cts.Token, active_session.CompletionToken);
                Assert.False(active_session.IsDefaultRunnerManagerUsed);
            }
        }

        [Fact]
        public void CreateRunner()
        {
            RunnerTestSetup test_setup = new RunnerTestSetup();
            Active_Session active_session=new Active_Session(test_setup.MockServiceScope.Object,
                test_setup.FakeStore.Object,
                test_setup.StubSession.Object,
                null);

            (var runner, var key) = active_session.CreateRunner<Request1, Result1>(test_setup.Request, test_setup.StubContext.Object);

            Assert.False(active_session.IsFresh);
            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal(RunnerTestSetup.TEST_RUNNER_NUMBER, key);
            Assert.Equal(test_setup.Request, ((SpyRunner1)runner).Request);

            active_session.SetDisposedForTests();
            Assert.Throws<ObjectDisposedException>(()=>active_session.CreateRunner<Request1, Result1>(test_setup.Request, test_setup.StubContext.Object));
        }

        [Fact]
        public void GetRunner()
        {
            RunnerTestSetup test_setup = new RunnerTestSetup();
            Active_Session active_session = new Active_Session(test_setup.MockServiceScope.Object,
                test_setup.FakeStore.Object,
                test_setup.StubSession.Object,
                null);

            var runner = active_session.GetRunner<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object);
            var unknown_runner = active_session.GetRunner<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER-1, test_setup.StubContext.Object);

            Assert.False(active_session.IsFresh);
            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal(test_setup.ExistingRunner, (SpyRunner1)runner);
            Assert.Null(unknown_runner);

            active_session.SetDisposedForTests();
            Assert.Throws<ObjectDisposedException>(() => active_session.GetRunner<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object));
        }

        [Fact]
        public void GetRunnerAsync()
        {
            RunnerTestSetup test_setup = new RunnerTestSetup();
            Active_Session active_session = new Active_Session(test_setup.MockServiceScope.Object,
                test_setup.FakeStore.Object,
                test_setup.StubSession.Object,
                null);

            var runner = active_session.GetRunnerAsync<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object, default).GetAwaiter().GetResult();
            var unknown_runner = active_session.GetRunnerAsync<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER-1, test_setup.StubContext.Object, default).GetAwaiter().GetResult();

            Assert.False(active_session.IsFresh);
            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal(test_setup.ExistingRunner, (SpyRunner1)runner);
            Assert.Null(unknown_runner);

            active_session.SetDisposedForTests();
            Assert.Throws<ObjectDisposedException>(() => active_session.GetRunnerAsync<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object, default).GetAwaiter().GetResult());
        }

        [Fact]
        public void Dispose()
        {
            ConstructorTestSetup test_setup;
            Active_Session active_session;
            Mock<IRunnerManager> mock_runner_manager;
            //Test case: disposing ActiveSession with internal runner manager
            mock_runner_manager= MockRunnerManager.CreateMockedRunnermanager();
            test_setup=new ConstructorTestSetup();
            active_session=new Active_Session(mock_runner_manager.Object, test_setup.MockServiceScope.Object,
                test_setup.FakeStore.Object,
                test_setup.StubSession.Object);

            active_session.Dispose();

            Assert.True(active_session.Disposed);
            mock_runner_manager.Verify(MockRunnerManager.WaitForRunnersExpression,Times.Once);
            test_setup.MockServiceScope.Verify(test_setup.DisposeScopeExpression, Times.Once);
            mock_runner_manager.As<IDisposable>().Verify(MockRunnerManager.DisposeExpression, Times.Once);

            //Test case: disposing ActiveSession with external runner manager
            mock_runner_manager=MockRunnerManager.CreateMockedRunnermanager();
            test_setup=new ConstructorTestSetup(mock_runner_manager.Object);
            active_session=new Active_Session( test_setup.MockServiceScope.Object,
                test_setup.FakeStore.Object,
                test_setup.StubSession.Object,
                null );

            active_session.Dispose();

            Assert.True(active_session.Disposed);
            mock_runner_manager.Verify(MockRunnerManager.WaitForRunnersExpression, Times.Once);
            test_setup.MockServiceScope.Verify(test_setup.DisposeScopeExpression, Times.Once);
            mock_runner_manager.As<IDisposable>().Verify(MockRunnerManager.DisposeExpression, Times.Never);

            //Test case: simulate disposing of an already disposed ActiveSession
            mock_runner_manager=MockRunnerManager.CreateMockedRunnermanager();
            test_setup=new ConstructorTestSetup();
            active_session=new Active_Session(mock_runner_manager.Object, test_setup.MockServiceScope.Object,
                test_setup.FakeStore.Object,
                test_setup.StubSession.Object);

            active_session.SetDisposedForTests();
            active_session.Dispose();

            Assert.True(active_session.Disposed);
        }

        //TODO Test case: ActiveSession.DisposeAsync() test
        // Assert.NotNull(active_session.GetCleanupCompletionTask());
        // active_session.GetCleanupCompletionTask()?.GetAwaiter().GetResult();
        //TODO Test case: Dispose ActiveSession with pendinding runners 
        //TODO Test case: DisposeAsync ActiveSession with pendinding runners 

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // DefaultRunnerManager tests
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        [Fact]
        public void CreateDefaultRunnermanager()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();

            using (Active_Session.DefaultRunnerManager manager = new Active_Session.DefaultRunnerManager("", null, dummy_sp.Object)) {
                Assert.Equal(dummy_sp.Object, manager.Services);
                Assert.NotNull(manager.RunnerCreationLock);
                Assert.NotNull(manager.RunnersCounter);
                Assert.Equal(1, manager.RunnersCounter.CurrentCount);
                Assert.True(manager.SessionCompletionToken.CanBeCanceled);
            }
        }

        [Fact]
        public void RegisterRunner()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            using (Active_Session.DefaultRunnerManager manager = new Active_Session.DefaultRunnerManager("", null, dummy_sp.Object)) {

                manager.RegisterRunner(0);

                Assert.Equal(2, manager.RunnersCounter.CurrentCount);
            }
        }

        [Fact]
        public void UnregisterRunner()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            using (Active_Session.DefaultRunnerManager manager = new Active_Session.DefaultRunnerManager("", null, dummy_sp.Object)) {
                manager.RegisterRunner(0);

                manager.UnregisterRunner(0);

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
            using (Active_Session.DefaultRunnerManager manager = new Active_Session.DefaultRunnerManager("", null, dummy_sp.Object,0,2)) {
                int number;
                number=manager.GetNewRunnerNumber();
                Assert.Equal(0, number);
                number=manager.GetNewRunnerNumber();
                Assert.Equal(1, number);
                Assert.Throws<InvalidOperationException>(()=>manager.GetNewRunnerNumber());
            }
        }

        [Fact]
        public void WaitForRunners()
        {
            Mock<IServiceProvider> dummy_sp = new Mock<IServiceProvider>();
            using (Active_Session.DefaultRunnerManager manager = new Active_Session.DefaultRunnerManager("", null, dummy_sp.Object)) {
                int runner_delay = 100;
                Task<Boolean> wait_for_runners_task = new Task<Boolean>(() => manager.WaitForRunners(runner_delay));
                using (ManualResetEventSlim runner_event = new ManualResetEventSlim(false)) {
                    Task runner_task = new Task(() => { runner_event.Wait(); manager.UnregisterRunner(0); });
                    manager.RegisterRunner(0);
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
            using (Active_Session.DefaultRunnerManager manager = new Active_Session.DefaultRunnerManager("", null, dummy_sp.Object)) {
                int runner_delay = 300;
                Task<Boolean> wait_for_runners_task = new Task<Boolean>(() => manager.WaitForRunners(runner_delay));
                using (CancellationTokenSource cts=new CancellationTokenSource(500)) {
                    CancellationToken ct = cts.Token;
                    Task runner_task = new Task(() => { 
                        while (!ct.IsCancellationRequested) Task.Delay(50).Wait();  
                        manager.UnregisterRunner(0); 
                    });
                    manager.RegisterRunner(0);
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
            Active_Session.DefaultRunnerManager manager = new Active_Session.DefaultRunnerManager("", null, dummy_sp.Object, 0, 2);

            manager.Dispose();
            Assert.Throws<ObjectDisposedException>(()=>manager.SessionCompletionToken);
            Assert.Throws<ObjectDisposedException>(() => manager.RegisterRunner(0));
        }

        class ConstructorTestSetup
        {
            public readonly Mock<IServiceProvider> StubServiceProvider;
            public readonly Mock<IServiceScope> MockServiceScope;
            public readonly Mock<IActiveSessionStore> FakeStore;
            public readonly Mock<ISession> StubSession;
            public readonly Mock<HttpContext> StubContext;
            public readonly Request1 Request;

            public const String TEST_SESSION_ID = "TestSessionId";
            public const String TEST_REQUEST_ARG = "TesRequestArg";
            public readonly Expression<Action<IServiceScope>> DisposeScopeExpression= s => s.Dispose();

            public ConstructorTestSetup(IRunnerManager? Manager = null)
            {
                StubServiceProvider=new Mock<IServiceProvider>();
                StubServiceProvider.Setup(s => s.GetService(typeof(IRunnerManager))).Returns(Manager);
                MockServiceScope=new Mock<IServiceScope>();
                MockServiceScope.SetupGet(s => s.ServiceProvider).Returns(StubServiceProvider.Object);
                MockServiceScope.Setup(DisposeScopeExpression);
                FakeStore=new Mock<IActiveSessionStore>();
                StubSession=new Mock<ISession>();
                StubSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                StubContext = new Mock<HttpContext>();
                StubContext.SetupGet(s=>s.Session).Returns(StubSession.Object);
                Request=new Request1 { Arg=TEST_REQUEST_ARG };
            }

        }

        class RunnerTestSetup : ConstructorTestSetup
        {
            public const Int32 TEST_RUNNER_NUMBER = 10;
            public const String EXISTING_TEST_RUNNER_ARG = "ExistingRunnerArg";

            readonly Expression<Func<IActiveSessionStore, KeyedActiveSessionRunner<Result1>>> _createRunnerExpression;
            readonly Expression<Func<IActiveSessionStore,IActiveSessionRunner<Result1>?>> _getRunnerExpression;
            readonly Expression<Func<IActiveSessionStore, ValueTask<IActiveSessionRunner<Result1>?>>> _getRunnerExpressionAsync;
            public readonly SpyRunner1 ExistingRunner = new SpyRunner1(new Request1 { Arg = EXISTING_TEST_RUNNER_ARG });

            public RunnerTestSetup() : base()
            {
                _createRunnerExpression=s => s.CreateRunner<Request1, Result1>(
                        StubSession.Object,
                        It.IsAny<IRunnerManager>(),
                        Request,
                        It.IsAny<String>()
                        );
                FakeStore.Setup(_createRunnerExpression)
                    .Returns((ISession _, IRunnerManager _, Request1 r, String _) => new KeyedActiveSessionRunner<Result1>(new SpyRunner1(r), TEST_RUNNER_NUMBER));
                FakeStore.Setup(s => s.GetRunner<Result1>(StubSession.Object, It.IsAny<IRunnerManager>(), It.IsAny<Int32>(), It.IsAny<String>()))
                    .Returns((IActiveSessionRunner<Result1>?)null);
                _getRunnerExpression=s => s.GetRunner<Result1>(StubSession.Object, It.IsAny<IRunnerManager>(), TEST_RUNNER_NUMBER, It.IsAny<String>());
                FakeStore.Setup(_getRunnerExpression).Returns(ExistingRunner);
                FakeStore.Setup(s => s.GetRunnerAsync<Result1>(StubSession.Object, It.IsAny<IRunnerManager>(), It.IsAny<Int32>(), It.IsAny<String>(),It.IsAny<CancellationToken>()))
                    .Returns(new ValueTask<IActiveSessionRunner<Result1>?>((IActiveSessionRunner<Result1>?)null));
                _getRunnerExpressionAsync=s => s.GetRunnerAsync<Result1>(StubSession.Object, It.IsAny<IRunnerManager>(), TEST_RUNNER_NUMBER, It.IsAny<String>(), It.IsAny<CancellationToken>());
                FakeStore.Setup(_getRunnerExpressionAsync).Returns(new ValueTask<IActiveSessionRunner<Result1>?>(ExistingRunner));
            }
        }

        static class MockRunnerManager {
            public static readonly Expression<Func<IRunnerManager, Boolean>> WaitForRunnersExpression = (IRunnerManager s) => s.WaitForRunners(It.IsAny<Int32>());
            public static readonly Expression<Action<IDisposable>> DisposeExpression = (IDisposable s) => s.Dispose();

            public static Mock<IRunnerManager> CreateMockedRunnermanager()
            {
                Mock<IRunnerManager> mock_runner_manager = new Mock<IRunnerManager>();
                mock_runner_manager.Setup(WaitForRunnersExpression).Returns(true);
                Mock<IDisposable> disposable_runner_manager = mock_runner_manager.As<IDisposable>();
                disposable_runner_manager.Setup(DisposeExpression);
                return mock_runner_manager;
            }
        }
    }
}
