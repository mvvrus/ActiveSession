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
            //Test case: create ActiveSessionFeture class instance
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

            //Test case: fill ActiveSession property, success
            test_setup= new ActiveSessionTestSetup();
            feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);
            active_session=feature.ActiveSession;
            Assert.True(active_session.IsAvailable);
            Assert.Equal(TEST_SESSION_ID, active_session.Id);

            //Test case: fill ActiveSession property, whlie ActiveSession class instance cannot be created
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
            //Test case: the ActiveSession instance is available
            feature.CommitAsync().GetAwaiter().GetResult();

            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Never);
            feature.Load();
            feature.CommitAsync().GetAwaiter().GetResult();
            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Once);

            //Test case: the ActiveSession instance is not available
            test_setup=new CommitAsyncTestSetup(SessionState.unavailable);
            feature=new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);
            feature.Load();
            feature.CommitAsync().GetAwaiter().GetResult();
            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Never);

            //Test case: no session
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


        [Fact]
        public void LoadAsync_Successful()
        {
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup();
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);

            feature.LoadAsync().GetAwaiter().GetResult();

            Assert.True(feature.IsLoaded);
            Assert.True(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);

            feature.LoadAsync().GetAwaiter().GetResult();
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }

        [Fact]
        public void LoadAsync_UnavailableSession()
        {
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(SessionState.unavailable);
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);


            feature.LoadAsync().GetAwaiter().GetResult();

            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Never);
        }

        [Fact]
        public void LoadAsync_NullSession()
        {
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(SessionState.absent);
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);


            feature.LoadAsync().GetAwaiter().GetResult();

            Assert.Null(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Never);
        }

        [Fact]
        public void LoadAsync_WithException()
        {
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(true);
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);


            feature.LoadAsync().GetAwaiter().GetResult();

            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }

        [Fact]
        public void Clear()
        {
            LoadTestSetup test_setup = new LoadTestSetup();
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);

            Assert.False(feature.IsLoaded);
            feature.Clear();
            Assert.False(feature.IsLoaded);
            Assert.Null(feature.Session);
            Assert.False(feature.RawActiveSession.IsAvailable);

            feature.Load();
            Assert.True(feature.IsLoaded);

            feature.Clear();
            Assert.False(feature.IsLoaded);
            Assert.Null(feature.Session);
            Assert.False(feature.RawActiveSession.IsAvailable);
        }

        [Fact]
        public void GetCurrentStoreStatistics()
        {
            CurrentStoreStatisticsSetup test_setup;
            IActiveSessionFeature feature;
            ActiveSessionStoreStats? statistics;

            //Test case: no statistics tracking
            test_setup=new CurrentStoreStatisticsSetup(false);
            feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);

            statistics = feature.GetCurrentStoreStatistics();

            Assert.Null(statistics);

            //Test case: statistics tracking active
            test_setup=new CurrentStoreStatisticsSetup(true);
            feature=new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);

            statistics = feature.GetCurrentStoreStatistics();

            Assert.NotNull(statistics);
            Assert.Equal(1, statistics.SessionCount);
            Assert.Equal(2, statistics.RunnerCount);
            Assert.Equal(0, statistics.StoreSize);
        }

        [Fact]
        public void SetSession()
        {
            const String ANOTHER_SESSION_ID = "ANOTHER_SESSION_ID";
            Mock<ISession> StubNewSession = new Mock<ISession>();
            StubNewSession.SetupGet(s => s.Id).Returns(ANOTHER_SESSION_ID);

            LoadTestSetup test_setup = new LoadTestSetup();
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);

            Assert.False(feature.IsLoaded);
            feature.SetSession(StubNewSession.Object, null);
            Assert.Equal(StubNewSession.Object, feature.Session);

            feature.Load();
            Assert.True(feature.IsLoaded);
            feature.SetSession(StubNewSession.Object, null);
            Assert.Equal(StubNewSession.Object, feature.Session);
            Assert.False(feature.IsLoaded);
            Assert.False(feature.RawActiveSession.IsAvailable);

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

        class LoadTestSetup : ConstructorTestSetup
        {
            public readonly Mock<IActiveSession> StubActiveSession;
            public readonly Expression<Func<IActiveSessionStore, IActiveSession>> ActiveSessionLoadExpression;

            public LoadTestSetup(Boolean Throws) : this(SessionState.normal, Throws) { }
            public LoadTestSetup() : this(SessionState.normal, false) { }
            public LoadTestSetup(SessionState State) : this(State, false) { }

            protected LoadTestSetup(SessionState State, Boolean Throws) : base(State)
            {
                StubActiveSession=new Mock<IActiveSession>();
                StubActiveSession.SetupGet(s => s.IsAvailable).Returns(true);
                ActiveSessionLoadExpression=s => s.FetchOrCreateSession(SessionObject!, It.IsAny<string>());
                if (Throws)
                    MockStore.Setup(ActiveSessionLoadExpression).Throws(new TestException());
                else
                    MockStore.Setup(ActiveSessionLoadExpression).Returns(StubActiveSession.Object);
            }
        }

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

        class LoadAsyncTestSetup: LoadTestSetup
        {
            public LoadAsyncTestSetup(Boolean Throws) : this(SessionState.normal, Throws) { }
            public LoadAsyncTestSetup() : this(SessionState.normal, false) { }
            public LoadAsyncTestSetup(SessionState State) : this(State, false) { }

            protected LoadAsyncTestSetup(SessionState State, Boolean Throws) : base(State,Throws) 
            {
                MockSession?.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            }

        }

        class CurrentStoreStatisticsSetup: ConstructorTestSetup
        {
            public CurrentStoreStatisticsSetup(Boolean TraceStatistics):base() 
            {
                MockStore.Setup(s => s.GetCurrentStatistics())
                    .Returns(TraceStatistics ? new ActiveSessionStoreStats() { 
                        SessionCount=1,RunnerCount=2,StoreSize=0} 
                    : null);
            }
        }

        class TestException : Exception
        {
            public TestException() : base() { }
        }

    }

}
