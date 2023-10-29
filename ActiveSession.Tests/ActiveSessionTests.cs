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
                //Test case: normal creation
                active_session=new Active_Session(test_setup.DummyRunnerManager.Object,
                    test_setup.MockServiceScope.Object,
                    test_setup.FakeStore.Object,
                    test_setup.StubSession.Object,
                    null);

                Assert.True(active_session.IsAvailable);
                Assert.Equal(ConstructorTestSetup.TEST_SESSION_ID, active_session.Id);
                Assert.Equal(test_setup.DummyRunnerManager.Object, active_session.RunnerManager);
                Assert.True(active_session.CompletionToken.CanBeCanceled);
                Assert.False(active_session.CompletionToken.IsCancellationRequested);
                Assert.Equal(test_setup.StubServiceProvider.Object, active_session.SessionServices);
                Assert.True(active_session.IsFresh);
                Assert.False(active_session.Disposed);

                //Test case: null RunnerManager constructor parameter
                Assert.Throws<ArgumentNullException>(
                    () => new Active_Session(null!,
                        test_setup.MockServiceScope.Object,
                        test_setup.FakeStore.Object,
                        test_setup.StubSession.Object,
                        null)
                    );

                //Test case: null SessionScope constructor parameter
                Assert.Throws<ArgumentNullException>(
                    () => new Active_Session(test_setup.DummyRunnerManager.Object,
                        null!,
                        test_setup.FakeStore.Object,
                        test_setup.StubSession.Object,
                        null)
                    );

                //Test case: null Store constructor parameter
                Assert.Throws<ArgumentNullException>(
                    () => new Active_Session(test_setup.DummyRunnerManager.Object,
                        test_setup.MockServiceScope.Object,
                        null!,
                        test_setup.StubSession.Object,
                        null)
                    );

                //Test case: null Session constructor parameter
                Assert.Throws<ArgumentNullException>(
                    () => new Active_Session(test_setup.DummyRunnerManager.Object,
                        test_setup.MockServiceScope.Object,
                        test_setup.FakeStore.Object,
                        null!,
                        null)
                    );

            }

        }

        [Fact]
        public void CreateRunner()
        {
            using (RunnerTestSetup test_setup = new RunnerTestSetup()) {

            Active_Session active_session=new Active_Session(test_setup.DummyRunnerManager.Object,
                test_setup.MockServiceScope.Object,
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
        }

        [Fact]
        public void GetRunner()
        {
            using (RunnerTestSetup test_setup = new RunnerTestSetup()) {
                Active_Session active_session = new Active_Session(test_setup.DummyRunnerManager.Object,
                    test_setup.MockServiceScope.Object,
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
        }

        [Fact]
        public void GetRunnerAsync()
        {
            using (RunnerTestSetup test_setup = new RunnerTestSetup()) {
                Active_Session active_session = new Active_Session(test_setup.DummyRunnerManager.Object,
                test_setup.MockServiceScope.Object,
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
                    test_setup.FakeStore.Object,
                    test_setup.StubSession.Object,
                    null);

                active_session.Dispose();

                Assert.True(active_session.Disposed);
                Assert.True(active_session.CompletionToken.IsCancellationRequested);
                test_setup.DummyRunnerManager.Verify(MockRunnerManager.WaitForRunnersExpression, Times.Never);
                test_setup.MockServiceScope.Verify(test_setup.DisposeScopeExpression, Times.Never);
                test_setup.DummyRunnerManager.As<IDisposable>().Verify(MockRunnerManager.DisposeExpression, Times.Never);
            }

            //Test case: simulate disposing of an already disposed ActiveSession
            using (test_setup=new ConstructorTestSetup()) {
                active_session=new Active_Session(test_setup.DummyRunnerManager.Object, test_setup.MockServiceScope.Object,
                    test_setup.FakeStore.Object,
                    test_setup.StubSession.Object,
                    null);

                active_session.SetDisposedForTests();
                active_session.Dispose();

                Assert.True(active_session.Disposed);
            }
        }

        //TODO Test case: ActiveSession.DisposeAsync() test
        // Assert.NotNull(active_session.GetCleanupCompletionTask());
        // active_session.GetCleanupCompletionTask()?.GetAwaiter().GetResult();
        //TODO Test case: Dispose ActiveSession with pendinding runners 
        //TODO Test case: DisposeAsync ActiveSession with pendinding runners 

        class ConstructorTestSetup: IDisposable
        {
            public readonly Mock<IServiceProvider> StubServiceProvider;
            public readonly Mock<IServiceScope> MockServiceScope;
            public readonly Mock<IActiveSessionStore> FakeStore;
            public readonly Mock<ISession> StubSession;
            public readonly Mock<HttpContext> StubContext;
            public readonly Request1 Request;
            public readonly Mock<IRunnerManager> DummyRunnerManager;
            readonly CancellationTokenSource _cts;
            public CancellationToken Token { get => _cts.Token; }

            public const String TEST_SESSION_ID = "TestSessionId";
            public const String TEST_REQUEST_ARG = "TesRequestArg";
            public readonly Expression<Action<IServiceScope>> DisposeScopeExpression= s => s.Dispose();

            public ConstructorTestSetup()
            {
                StubServiceProvider=new Mock<IServiceProvider>();
                MockServiceScope=new Mock<IServiceScope>();
                MockServiceScope.SetupGet(s => s.ServiceProvider).Returns(StubServiceProvider.Object);
                MockServiceScope.Setup(DisposeScopeExpression);
                FakeStore=new Mock<IActiveSessionStore>();
                StubSession=new Mock<ISession>();
                StubSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                StubContext = new Mock<HttpContext>();
                StubContext.SetupGet(s=>s.Session).Returns(StubSession.Object);
                DummyRunnerManager=MockRunnerManager.CreateMockedRunnermanager();
                _cts = new CancellationTokenSource();
                DummyRunnerManager.SetupGet(s => s.CompletionToken).Returns(_cts.Token);
                Request=new Request1 { Arg=TEST_REQUEST_ARG };
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

            readonly Expression<Func<IActiveSessionStore, KeyedActiveSessionRunner<Result1>>> _createRunnerExpression;
            readonly Expression<Func<IActiveSessionStore,IActiveSessionRunner<Result1>?>> _getRunnerExpression;
            readonly Expression<Func<IActiveSessionStore, ValueTask<IActiveSessionRunner<Result1>?>>> _getRunnerExpressionAsync;
            public readonly SpyRunner1 ExistingRunner = new SpyRunner1(new Request1 { Arg = EXISTING_TEST_RUNNER_ARG });

            public RunnerTestSetup() : base()
            {
                _createRunnerExpression=s => s.CreateRunner<Request1, Result1>(
                        StubSession.Object,
                        It.IsAny<IActiveSession>(),
                        It.IsAny<IRunnerManager>(),
                        Request,
                        It.IsAny<String>()
                        );
                FakeStore.Setup(_createRunnerExpression)
                    .Returns((ISession _, IActiveSession _, IRunnerManager _, Request1 r, String _) => new KeyedActiveSessionRunner<Result1>(new SpyRunner1(r), TEST_RUNNER_NUMBER));
                FakeStore.Setup(s => s.GetRunner<Result1>(StubSession.Object, It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), It.IsAny<Int32>(), It.IsAny<String>()))
                    .Returns((IActiveSessionRunner<Result1>?)null);
                _getRunnerExpression=s => s.GetRunner<Result1>(StubSession.Object, It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), TEST_RUNNER_NUMBER, It.IsAny<String>());
                FakeStore.Setup(_getRunnerExpression).Returns(ExistingRunner);
                FakeStore.Setup(s => s.GetRunnerAsync<Result1>(StubSession.Object, It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), It.IsAny<Int32>(), It.IsAny<String>(),It.IsAny<CancellationToken>()))
                    .Returns(new ValueTask<IActiveSessionRunner<Result1>?>((IActiveSessionRunner<Result1>?)null));
                _getRunnerExpressionAsync=s => s.GetRunnerAsync<Result1>(StubSession.Object, It.IsAny<IActiveSession>(), It.IsAny<IRunnerManager>(), TEST_RUNNER_NUMBER, It.IsAny<String>(), It.IsAny<CancellationToken>());
                FakeStore.Setup(_getRunnerExpressionAsync).Returns(new ValueTask<IActiveSessionRunner<Result1>?>(ExistingRunner));
            }
        }

        static class MockRunnerManager {
            public static readonly Expression<Func<IRunnerManager, Boolean>> WaitForRunnersExpression = (IRunnerManager s) => s.WaitForRunners(It.IsAny<IActiveSession>(), It.IsAny<Int32>());
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
