using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession;
using MVVrus.AspNetCore.ActiveSession.Internal;
using Active_Session= MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession;

namespace ActiveSession.Tests
{
    public class ActiveSessionTests
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // IActiveSession tests
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        [Fact]
        public void CreateActiveSession()
        {
            ConstructorTestSetup test_setup;
            Active_Session active_session;

            using (test_setup=new ConstructorTestSetup()) {
                //Test case: normal creation w/o specific cleanup completion task
                active_session=new Active_Session(test_setup.DummyRunnerManager.Object,
                    test_setup.MockServiceScope.Object,
                    test_setup.MockStore.Object,
                    test_setup.StubSession.Object,
                    test_setup.Logger);

                Assert.True(active_session.IsAvailable);
                Assert.Equal(ConstructorTestSetup.TEST_SESSION_ID, active_session.Id);
                Assert.Equal(test_setup.DummyRunnerManager.Object, active_session.RunnerManager);
                Assert.True(active_session.CompletionToken.CanBeCanceled);
                Assert.False(active_session.CompletionToken.IsCancellationRequested);
                Assert.Equal(test_setup.StubServiceProvider.Object, active_session.SessionServices);
                Assert.True(active_session.IsFresh);
                Assert.False(active_session.Disposed);
                Assert.True(active_session.CleanupCompletionTask.IsCompletedSuccessfully);

                //Test case: normal creation with specific cleanup completion task
                Task<Boolean> dummy_completion_task = Task.FromCanceled<Boolean>(new CancellationToken(true));

                active_session=new Active_Session(test_setup.DummyRunnerManager.Object,
                    test_setup.MockServiceScope.Object,
                    test_setup.MockStore.Object,
                    test_setup.StubSession.Object,
                    test_setup.Logger,
                    dummy_completion_task);

                Assert.True(ReferenceEquals(dummy_completion_task, active_session.CleanupCompletionTask));

                //Test case: null RunnerManager constructor parameter
                Assert.Throws<ArgumentNullException>(
                    () => new Active_Session(null!,
                        test_setup.MockServiceScope.Object,
                        test_setup.MockStore.Object,
                        test_setup.StubSession.Object,
                        test_setup.Logger)
                    );

                //Test case: null SessionScope constructor parameter
                Assert.Throws<ArgumentNullException>(
                    () => new Active_Session(test_setup.DummyRunnerManager.Object,
                        null!,
                        test_setup.MockStore.Object,
                        test_setup.StubSession.Object,
                        test_setup.Logger)
                    );

                //Test case: null Store constructor parameter
                Assert.Throws<ArgumentNullException>(
                    () => new Active_Session(test_setup.DummyRunnerManager.Object,
                        test_setup.MockServiceScope.Object,
                        null!,
                        test_setup.StubSession.Object,
                        test_setup.Logger)
                    );

                //Test case: null Session constructor parameter
                Assert.Throws<ArgumentNullException>(
                    () => new Active_Session(test_setup.DummyRunnerManager.Object,
                        test_setup.MockServiceScope.Object,
                        test_setup.MockStore.Object,
                        null!,
                        test_setup.Logger)
                    );
            }

        }

        [Fact]
        public void CreateRunner()
        {
            //Test case: normal runner creation
            using (RunnerTestSetup test_setup = new RunnerTestSetup()) {

                Active_Session active_session = new Active_Session(test_setup.DummyRunnerManager.Object,
                    test_setup.MockServiceScope.Object,
                    test_setup.MockStore.Object,
                    test_setup.StubSession.Object,
                    test_setup.Logger);

                (var runner, var key)=active_session.CreateRunner<Request1, Result1>(test_setup.Request, test_setup.StubContext.Object);

                Assert.False(active_session.IsFresh);
                Assert.NotNull(runner);
                Assert.IsType<SpyRunner1>(runner);
                Assert.Equal(RunnerTestSetup.TEST_RUNNER_NUMBER, key);
                Assert.Equal(test_setup.Request, ((SpyRunner1)runner).Request);

                //Test case: runner creation after ActiveSession disposal
                active_session.SetDisposedForTests();
                Assert.Throws<ObjectDisposedException>(() => active_session.CreateRunner<Request1, Result1>(test_setup.Request, test_setup.StubContext.Object));
            }
        }

        [Fact]
        public void GetRunner()
        {
            //Test case: successful and unsuccessful runner search
            using (RunnerTestSetup test_setup = new RunnerTestSetup()) {
                Active_Session active_session = new Active_Session(test_setup.DummyRunnerManager.Object,
                    test_setup.MockServiceScope.Object,
                    test_setup.MockStore.Object,
                    test_setup.StubSession.Object,
                    test_setup.Logger);

                var runner = active_session.GetRunner<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object);
                var unknown_runner = active_session.GetRunner<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER-1, test_setup.StubContext.Object);

                Assert.False(active_session.IsFresh);
                Assert.NotNull(runner);
                Assert.IsType<SpyRunner1>(runner);
                Assert.Equal(test_setup.ExistingRunner, (SpyRunner1)runner);
                Assert.Null(unknown_runner);

                //Test case: runner search after disposal
                active_session.SetDisposedForTests();
                Assert.Throws<ObjectDisposedException>(() => active_session.GetRunner<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object));

            }
        }

        [Fact]
        public void GetRunnerAsync()
        {
            //Test case: successful and unsuccessful async runner search
            using (RunnerTestSetup test_setup = new RunnerTestSetup()) {
                Active_Session active_session = new Active_Session(test_setup.DummyRunnerManager.Object,
                test_setup.MockServiceScope.Object,
                test_setup.MockStore.Object,
                test_setup.StubSession.Object,
                test_setup.Logger);

                var runner = active_session.GetRunnerAsync<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object, default).GetAwaiter().GetResult();
                var unknown_runner = active_session.GetRunnerAsync<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER-1, test_setup.StubContext.Object, default).GetAwaiter().GetResult();

                Assert.False(active_session.IsFresh);
                Assert.NotNull(runner);
                Assert.IsType<SpyRunner1>(runner);
                Assert.Equal(test_setup.ExistingRunner, (SpyRunner1)runner);
                Assert.Null(unknown_runner);

                //Test case: async runner search after disposal
                active_session.SetDisposedForTests();
                Assert.Throws<ObjectDisposedException>(() => active_session.GetRunnerAsync<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object, default).GetAwaiter().GetResult());
            }
        }

        [Fact]
        public void Dispose()
        {
            ConstructorTestSetup test_setup;
            Active_Session active_session;
            //Test case: disposing ActiveSession once
            using (test_setup=new ConstructorTestSetup()) {
                active_session=new Active_Session(test_setup.DummyRunnerManager.Object,
                    test_setup.MockServiceScope.Object,
                    test_setup.MockStore.Object,
                    test_setup.StubSession.Object,
                    test_setup.Logger);
                Boolean called_back = false;
                active_session.CompletionToken.Register(() => { if(!called_back) called_back=true; });

                active_session.Dispose();

                Assert.True(active_session.Disposed);
                Assert.True(active_session.CompletionToken.IsCancellationRequested);
                Assert.True(called_back);
                test_setup.DummyRunnerManager.Verify(MockRunnerManager.PerformRunnersCleanupExpression, Times.Never);
                test_setup.MockServiceScope.Verify(test_setup.DisposeScopeExpression, Times.Never);
                test_setup.DummyRunnerManager.As<IDisposable>().Verify(MockRunnerManager.DisposeExpression, Times.Never);
            }

            //Test case: simulate disposing an already disposed ActiveSession
            using (test_setup=new ConstructorTestSetup()) {
                active_session=new Active_Session(test_setup.DummyRunnerManager.Object, test_setup.MockServiceScope.Object,
                    test_setup.MockStore.Object,
                    test_setup.StubSession.Object,
                    test_setup.Logger);

                active_session.SetDisposedForTests();
                active_session.Dispose();

                Assert.True(active_session.Disposed);
            }
        }

        [Fact]
        public void Terminate()
        {
            TerminateTestSetup test_setup;
            Task task;
            Active_Session active_session;

            //Test case: Terminate - simulate all runners have been completed in time
            using(test_setup=new TerminateTestSetup()) {
                active_session=new Active_Session(test_setup.DummyRunnerManager.Object,
                    test_setup.MockServiceScope.Object,
                    test_setup.MockStore.Object,
                    test_setup.StubSession.Object,
                    test_setup.Logger,
                    test_setup.CleanupCompletionTask);

                task=active_session.Terminate(test_setup.StubContext.Object);

                Assert.False(task.IsCompleted);
                test_setup.Complete();
                Assert.True(task.IsCompletedSuccessfully);
                test_setup.MockStore.Verify(test_setup.StoreTerminateExpression, Times.Once);
            }

            //Test case: Terminate - call on disposed ActiveSession
            using (ConstructorTestSetup ts=new ConstructorTestSetup()) {
                ts.MockStore.Setup(s => s.TerminateSession(It.IsAny<ISession>(), It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), It.IsAny<String>()))
                    .Returns(Task.FromResult(true));
                active_session=new Active_Session(ts.DummyRunnerManager.Object,
                    ts.MockServiceScope.Object,
                    ts.MockStore.Object,
                    ts.StubSession.Object,
                    null);
                active_session.SetDisposedForTests();

                task=active_session.Terminate(test_setup.StubContext.Object);

                Assert.True(task.IsCompletedSuccessfully);
            }

        }

        class ConstructorTestSetup: IDisposable
        {
            public readonly Mock<IServiceProvider> StubServiceProvider;
            public readonly Mock<IServiceScope> MockServiceScope;
            public readonly Mock<IActiveSessionStore> MockStore;
            public readonly Mock<ISession> StubSession;
            public readonly Request1 Request;
            public readonly Mock<IRunnerManager> DummyRunnerManager;
            readonly CancellationTokenSource _cts;
            public CancellationToken Token { get => _cts.Token; }

            public const String TEST_SESSION_ID = "TestSessionId";
            public const String TEST_REQUEST_ARG = "TesRequestArg";
            public readonly Expression<Action<IServiceScope>> DisposeScopeExpression= s => s.Dispose();
            readonly MockedLogger _mockedLogger;
            public ILogger Logger { get=>_mockedLogger.Logger; }

            public ConstructorTestSetup()
            {
                StubServiceProvider=new Mock<IServiceProvider>();
                MockServiceScope=new Mock<IServiceScope>();
                MockServiceScope.SetupGet(s => s.ServiceProvider).Returns(StubServiceProvider.Object);
                MockServiceScope.Setup(DisposeScopeExpression);
                MockStore=new Mock<IActiveSessionStore>();
                StubSession=new Mock<ISession>();
                StubSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                DummyRunnerManager=MockRunnerManager.CreateMockedRunnermanager();
                _cts = new CancellationTokenSource();
                Request=new Request1 { Arg=TEST_REQUEST_ARG };
                _mockedLogger=new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
            }

            public void Dispose()
            {
                _cts?.Dispose();
            }
        }

        class RunnerTestSetup : ConstructorTestSetup
        {
            public const Int32 TEST_RUNNER_NUMBER = 10;
            public const String EXISTING_TEST_RUNNER_ARG = "ExistingRunnerArg";

            readonly Expression<Func<IActiveSessionStore, KeyedRunner<Result1>>> _createRunnerExpression;
            readonly Expression<Func<IActiveSessionStore,IRunner<Result1>?>> _getRunnerExpression;
            readonly Expression<Func<IActiveSessionStore, Task<IRunner<Result1>?>>> _getRunnerExpressionAsync;
            public readonly IRunner<Result1> ExistingRunner = new SpyRunner1(new Request1 { Arg = EXISTING_TEST_RUNNER_ARG });
            public readonly Mock<HttpContext> StubContext;

            public RunnerTestSetup() : base()
            {
                _createRunnerExpression=s => s.CreateRunner<Request1, Result1>(
                        StubSession.Object,
                        It.IsAny<IActiveSession>(),
                        It.IsAny<IRunnerManager>(),
                        Request,
                        It.IsAny<String>()
                        );
                MockStore.Setup(_createRunnerExpression)
                    .Returns((ISession _, IActiveSession _, IRunnerManager _, Request1 r, String _) => new KeyedRunner<Result1>(new SpyRunner1(r), TEST_RUNNER_NUMBER));
                MockStore.Setup(s => s.GetRunner<Result1>(StubSession.Object, It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), It.IsAny<Int32>(), It.IsAny<String>()))
                    .Returns((IRunner<Result1>?)null);
                _getRunnerExpression=s => s.GetRunner<Result1>(StubSession.Object, It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), TEST_RUNNER_NUMBER, It.IsAny<String>());
                MockStore.Setup(_getRunnerExpression).Returns(ExistingRunner);
                MockStore.Setup(s => s.GetRunnerAsync<Result1>(StubSession.Object, It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), It.IsAny<Int32>(), It.IsAny<String>(),It.IsAny<CancellationToken>()))
                    .Returns(Task<IRunner<Result1>?>.FromResult((IRunner<Result1>?)null));
                _getRunnerExpressionAsync=s => s.GetRunnerAsync<Result1>(StubSession.Object, It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), TEST_RUNNER_NUMBER, It.IsAny<String>(), It.IsAny<CancellationToken>());
                MockStore.Setup(_getRunnerExpressionAsync).Returns(Task<IRunner<Result1>?>.FromResult((IRunner<Result1>?)ExistingRunner));
                StubContext=new Mock<HttpContext>();
                StubContext.SetupGet(s => s.Session).Returns(StubSession.Object);
            }
        }

        class TerminateTestSetup: ConstructorTestSetup
        {
            readonly TaskCompletionSource _tcs;
            public Task CleanupCompletionTask { get {return _tcs.Task; } }
            public readonly Mock<HttpContext> StubContext;
            public Expression<Func<IActiveSessionStore, Task>>? StoreTerminateExpression = null;

            public TerminateTestSetup(): base()
            {
                StubContext = new Mock<HttpContext>();
                StubContext.SetupGet(s => s.Session).Returns(StubSession.Object);
                _tcs=new TaskCompletionSource();
                MockStore.Setup(s => s.TerminateSession(It.IsAny<ISession>(), It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), It.IsAny<String>()))
                    .Returns(Task.CompletedTask);
                StoreTerminateExpression=s => s.TerminateSession(StubContext.Object.Session, It.IsAny<IActiveSession>(), DummyRunnerManager.Object, It.IsAny<String>());
                MockStore.Setup(StoreTerminateExpression).Returns(CleanupCompletionTask);
            }

            public void Complete()
            {
                _tcs.SetResult();
            }
        }

        static class MockRunnerManager {
            public static readonly Expression<Func<IRunnerManager, Task>> PerformRunnersCleanupExpression = (IRunnerManager s) => s.PerformRunnersCleanupAsync(It.IsAny<IActiveSession>());
            public static readonly Expression<Action<IDisposable>> DisposeExpression = (IDisposable s) => s.Dispose();

            public static Mock<IRunnerManager> CreateMockedRunnermanager()
            {
                Mock<IRunnerManager> mock_runner_manager = new Mock<IRunnerManager>();
                mock_runner_manager.Setup(PerformRunnersCleanupExpression).Returns(Task.CompletedTask);
                Mock<IDisposable> disposable_runner_manager = mock_runner_manager.As<IDisposable>();
                disposable_runner_manager.Setup(DisposeExpression);
                return mock_runner_manager;
            }
        }
    }
}
