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

            active_session=new Active_Session(test_setup.StubServiceScope.Object,
                test_setup.MockStore.Object,
                test_setup.StubSession.Object,
                null);

            Assert.True(active_session.IsAvailable);
            Assert.Equal(ConstructorTestSetup.TEST_SESSION_ID, active_session.Id);
            Assert.Equal(test_setup.StubServiceProvider.Object, active_session.SessionServices);
            Assert.True(active_session.IsFresh);
            Assert.IsType<Active_Session.DefaultRunnerManager>(active_session.RunnerManager);
            Assert.True(active_session.IsDefaultRunnerManagerUsed);

            Mock<IRunnerManager> fake_runner_manager = new Mock<IRunnerManager>();
            test_setup=new ConstructorTestSetup(fake_runner_manager.Object);
            active_session=new Active_Session(test_setup.StubServiceScope.Object,
                test_setup.MockStore.Object,
                test_setup.StubSession.Object,
                null);
            Assert.Equal(fake_runner_manager.Object, active_session.RunnerManager);
            Assert.False(active_session.IsDefaultRunnerManagerUsed);
        }

        [Fact]
        public void CreateRunner()
        {
            RunnerTestSetup test_setup = new RunnerTestSetup();
            Active_Session active_session=new Active_Session(test_setup.StubServiceScope.Object,
                test_setup.MockStore.Object,
                test_setup.StubSession.Object,
                null);

            (var runner, var key) = active_session.CreateRunner<Request1, Result1>(test_setup.Request, test_setup.StubContext.Object);

            Assert.False(active_session.IsFresh);
            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal(RunnerTestSetup.TEST_RUNNER_NUMBER, key);
            Assert.Equal(test_setup.Request, ((SpyRunner1)runner).Request);
        }

        [Fact]
        public void GetRunner()
        {
            RunnerTestSetup test_setup = new RunnerTestSetup();
            Active_Session active_session = new Active_Session(test_setup.StubServiceScope.Object,
                test_setup.MockStore.Object,
                test_setup.StubSession.Object,
                null);

            var runner = active_session.GetRunner<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object);
            var unknown_runner = active_session.GetRunner<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER-1, test_setup.StubContext.Object);

            Assert.False(active_session.IsFresh);
            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal(test_setup.ExistingRunner, (SpyRunner1)runner);
            Assert.Null(unknown_runner);
        }

        [Fact]
        public void GetRunnerAsync()
        {
            RunnerTestSetup test_setup = new RunnerTestSetup();
            Active_Session active_session = new Active_Session(test_setup.StubServiceScope.Object,
                test_setup.MockStore.Object,
                test_setup.StubSession.Object,
                null);

            var runner = active_session.GetRunnerAsync<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER, test_setup.StubContext.Object, default).GetAwaiter().GetResult();
            var unknown_runner = active_session.GetRunnerAsync<Result1>(RunnerTestSetup.TEST_RUNNER_NUMBER-1, test_setup.StubContext.Object, default).GetAwaiter().GetResult();

            Assert.False(active_session.IsFresh);
            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal(test_setup.ExistingRunner, (SpyRunner1)runner);
            Assert.Null(unknown_runner);
        }

        void CompletionToken()
        {
            //TODO
        }

        void SignalCompletion()
        {
            //TODO
        }

        void Dispose()
        {
            //TODO
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // IActiveSession tests
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        void CreateRunnermanager()
        {
            //TODO
            //Services
            //RunnerCreationLock
            //readonly CountdownEvent _runnersCounter;
            //readonly String _sessionId;
        }

        void RegisterRunner()
        {
            //TODO
        }

        void UnregisterRunner()
        {
            //TODO
        }

        void ReturnRunnerNumber()
        {
            //TODO
        }

        void GetNewRunnerNumber()
        {
            //TODO
        }

        void WaitForRunners()
        {
            //TODO
        }

        void Dispose_RunnerManager()
        {
            //TODO
        }

        class ConstructorTestSetup
        {
            public readonly Mock<IServiceProvider> StubServiceProvider;
            public readonly Mock<IServiceScope> StubServiceScope;
            public readonly Mock<IActiveSessionStore> MockStore;
            public readonly Mock<ISession> StubSession;
            public readonly Mock<HttpContext> StubContext;
            public readonly Request1 Request;

            public const String TEST_SESSION_ID = "TestSessionId";
            public const String TEST_REQUEST_ARG = "TesRequestArg";

            public ConstructorTestSetup(IRunnerManager? Manager = null)
            {
                StubServiceProvider=new Mock<IServiceProvider>();
                StubServiceProvider.Setup(s => s.GetService(typeof(IRunnerManager))).Returns(Manager);
                StubServiceScope=new Mock<IServiceScope>();
                StubServiceScope.SetupGet(s => s.ServiceProvider).Returns(StubServiceProvider.Object);
                MockStore=new Mock<IActiveSessionStore>();
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
                MockStore.Setup(_createRunnerExpression)
                    .Returns((ISession _, IRunnerManager _, Request1 r, String _) => new KeyedActiveSessionRunner<Result1>(new SpyRunner1(r), TEST_RUNNER_NUMBER));
                MockStore.Setup(s => s.GetRunner<Result1>(StubSession.Object, It.IsAny<IRunnerManager>(), It.IsAny<Int32>(), It.IsAny<String>()))
                    .Returns((IActiveSessionRunner<Result1>?)null);
                _getRunnerExpression=s => s.GetRunner<Result1>(StubSession.Object, It.IsAny<IRunnerManager>(), TEST_RUNNER_NUMBER, It.IsAny<String>());
                MockStore.Setup(_getRunnerExpression).Returns(ExistingRunner);
                MockStore.Setup(s => s.GetRunnerAsync<Result1>(StubSession.Object, It.IsAny<IRunnerManager>(), It.IsAny<Int32>(), It.IsAny<String>(),It.IsAny<CancellationToken>()))
                    .Returns(new ValueTask<IActiveSessionRunner<Result1>?>((IActiveSessionRunner<Result1>?)null));
                _getRunnerExpressionAsync=s => s.GetRunnerAsync<Result1>(StubSession.Object, It.IsAny<IRunnerManager>(), TEST_RUNNER_NUMBER, It.IsAny<String>(), It.IsAny<CancellationToken>());
                MockStore.Setup(_getRunnerExpressionAsync).Returns(new ValueTask<IActiveSessionRunner<Result1>?>(ExistingRunner));
            }

        }
    }
}
