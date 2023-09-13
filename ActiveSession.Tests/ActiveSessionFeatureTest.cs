using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MVVrus.AspNetCore.ActiveSession.Internal;


namespace ActiveSession.Tests
{
    public class ActiveSessionFeatureTest
    {
        const String TEST_TRACE_IDENTIFIER = "TEST_TRACE_IDENTIFIER";

        [Fact]
        public void CreateActiveSessionFeature()
        {
            ConstructorTestSetup test_setup = new ConstructorTestSetup();

            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);

            Assert.Equal(test_setup.MockStore.Object, feature.Store);
            Assert.Equal(test_setup.SessionObject, feature.Session);
            Assert.Null(feature.Logger);
            Assert.Equal(TEST_TRACE_IDENTIFIER, feature.TraceIdentifier);
            Assert.Equal(ActiveSessionFeature.DummySession, feature.RawActiveSession);
            Assert.False(feature.IsLoaded);
        }

        [Fact]
        public void ActiveSession()
        {
            ActiveSessionTestSetup test_setup;
            ActiveSessionFeature feature;
            IActiveSession active_session;

            test_setup= new ActiveSessionTestSetup();
            feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);
            active_session=feature.ActiveSession;
            Assert.True(active_session.IsAvailable);
            Assert.Equal(TEST_SESSION_ID, active_session.Id);

            test_setup=new ActiveSessionTestSetup(SessionState.unavailable);
            feature=new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);
            active_session=feature.ActiveSession;
            Assert.False(active_session.IsAvailable);
        }

        [Fact]
        public void CommitAsync()
        {
            CommitAsyncTestSetup test_setup = new CommitAsyncTestSetup(SessionState.normal);
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);
            feature.CommitAsync().GetAwaiter().GetResult();

            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Never);
            feature.Load();
            feature.CommitAsync().GetAwaiter().GetResult();
            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Once);

            test_setup=new CommitAsyncTestSetup(SessionState.unavailable);
            feature=new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);
            feature.Load();
            feature.CommitAsync().GetAwaiter().GetResult();
            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Never);

            test_setup=new CommitAsyncTestSetup(SessionState.absent);
            feature=new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);
            feature.Load();
            feature.CommitAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public void Load_Successful()
        {
            LoadTestSetup test_setup = new LoadTestSetup();
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);

            feature.Load();

            Assert.True(feature.IsLoaded);
            Assert.True(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);

            feature.Load();
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }

        [Fact]
        public void Load_UnavailableSession()
        {
            LoadTestSetup test_setup = new LoadTestSetup(SessionState.unavailable);
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);


            feature.Load();

            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Never);
        }

        [Fact]
        public void Load_NullSession()
        {
            LoadTestSetup test_setup = new LoadTestSetup(SessionState.absent);
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);


            feature.Load();

            Assert.Null(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Never);
        }

        [Fact]
        public void Load_WithException()
        {
            LoadTestSetup test_setup = new LoadTestSetup(true);
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);


            feature.Load();

            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }


        enum SessionState { normal, absent, unavailable }

        class ConstructorTestSetup
        {
            public readonly Mock<IActiveSessionStore> MockStore;
            public readonly Mock<ISession>? MockSession;
            public ISession? SessionObject { get {return MockSession?.Object; } }

            public ConstructorTestSetup() : this(SessionState.normal) { }

            protected  ConstructorTestSetup(SessionState State)
            {
                MockStore=new Mock<IActiveSessionStore>();
                if(State!=SessionState.absent) {
                    MockSession=new Mock<ISession>();
                    MockSession.SetupGet(s => s.IsAvailable).Returns(State==SessionState.normal);
                }
            }
        }

        const String TEST_SESSION_ID = "TEST_SESSION_ID";

        class ActiveSessionTestSetup : LoadTestSetup
        {
            public ActiveSessionTestSetup(SessionState State = SessionState.normal) : base(State)
            {
                StubActiveSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
            }
        }

        class CommitAsyncTestSetup : ConstructorTestSetup
        {
            public readonly Expression<Action<ISession>> SessionCommitAsyncExpression = s => s.CommitAsync(It.IsAny<CancellationToken>());
            public CommitAsyncTestSetup(SessionState State) : base(State) 
            {
                MockSession?.Setup(SessionCommitAsyncExpression);
            }
            
        }


        class LoadTestSetup: ConstructorTestSetup
        {
            public readonly Mock<IActiveSession> StubActiveSession;
            public readonly Expression<Func<IActiveSessionStore,IActiveSession>> ActiveSessionLoadExpression;

            public LoadTestSetup(Boolean Throws) : this(SessionState.normal, Throws) { }
            public LoadTestSetup() : this(SessionState.normal, false) { }
            public LoadTestSetup(SessionState State):this(State, false) { }

            protected LoadTestSetup(SessionState State, Boolean Throws):base(State)
            {
                StubActiveSession=new Mock<IActiveSession>();
                StubActiveSession.SetupGet(s=>s.IsAvailable).Returns(true);
                ActiveSessionLoadExpression = s => s.FetchOrCreateSession(SessionObject!, It.IsAny<string>());
                if (Throws)  MockStore.Setup(ActiveSessionLoadExpression).Throws(new TestException());
                else MockStore.Setup(ActiveSessionLoadExpression).Returns(StubActiveSession.Object);
            }
        }

        class TestException : Exception
        {
            public TestException() : base() { }
        }

    }

}
