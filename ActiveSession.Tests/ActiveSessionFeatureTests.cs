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
    public class ActiveSessionFeatureTests
    {
        const String TEST_TRACE_IDENTIFIER = "TEST_TRACE_IDENTIFIER";

        //Test case: create ActiveSessionFeture class instance
        [Fact]
        public void CreateActiveSessionFeature()
        {
            //Arrange
            ConstructorTestSetup test_setup = new ConstructorTestSetup();
            //Act
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Assess
            Assert.Equal(test_setup.MockStore.Object, feature.Store);
            Assert.Equal(test_setup.SessionObject, feature.Session);
            Assert.NotNull(feature.Logger);
            Assert.Equal(TEST_TRACE_IDENTIFIER, feature.TraceIdentifier);
            Assert.Equal(ActiveSessionFeature.DummySession, feature.RawActiveSession);
            Assert.False(feature.IsLoaded);
        }

        //Test group: Access to the ActiveSession property
        [Fact]
        public void ActiveSession()
        {
            ActiveSessionTestSetup test_setup;
            ActiveSessionFeature feature;
            IActiveSession active_session;

            //Test case: fill ActiveSession property, success
            //Arrange
            test_setup= new ActiveSessionTestSetup();
            feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            active_session=feature.ActiveSession;
            //Assess
            Assert.True(active_session.IsAvailable);
            Assert.Equal(TEST_SESSION_ID, active_session.Id);

            //Test case: fill ActiveSession property, whlie ActiveSession class instance cannot be created
            //Arrange
            test_setup=new ActiveSessionTestSetup(SessionState.unavailable);
            feature=new ActiveSessionFeature(test_setup.MockStore.Object, test_setup.SessionObject, null, TEST_TRACE_IDENTIFIER);
            //Act
            active_session=feature.ActiveSession;
            //Assess
            Assert.False(active_session.IsAvailable);
        }

        //Test group: Test CommitAsync method
        [Fact]
        public void CommitAsync()
        {
            //Test case: the ActiveSession instance is available
            //Arrange
            CommitAsyncTestSetup test_setup = new CommitAsyncTestSetup(SessionState.normal);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.CommitAsync().GetAwaiter().GetResult();
            //Assess
            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Never);
            feature.Load();
            feature.CommitAsync().GetAwaiter().GetResult();
            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Once);

            //Test case: the ActiveSession instance is not available
            //Arrange
            test_setup=new CommitAsyncTestSetup(SessionState.unavailable);
            feature=new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            feature.Load();
            //Act
            feature.CommitAsync().GetAwaiter().GetResult();
            //Asses
            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Never);

            //Test case: no session
            //Arrange
            test_setup=new CommitAsyncTestSetup(SessionState.absent);
            feature=new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            feature.Load();
            //Act and assess (just no exeption condition is required)
            feature.CommitAsync().GetAwaiter().GetResult();
        }

        //Test group: successfull execution of Load method
        [Fact]
        public void Load_Successful()
        {
            //Test case: the successfull first execution of Load method
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup();
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.Load();
            //Assess
            Assert.True(feature.IsLoaded);
            Assert.True(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);

            //Test case: successfull non-first executions of Load method (already Arranged)
            //Act
            feature.Load();
            //Assess
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }

        //Test case: call Load method with an unavailable ISession
        [Fact]
        public void Load_UnavailableSession()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(SessionState.unavailable);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.Load();
            //Assess
            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.NotNull(feature.ActiveSession);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Never);
        }

        //Test case: call Load method with a null ISession
        [Fact]
        public void Load_NullSession()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(SessionState.absent);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.Load();
            //Assess
            Assert.Null(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Never);
        }

        //Test case: call Load method leading to a null IActiveSession
        [Fact]
        public void Load_NullActiveSession()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(ActiveSessionState.isnull);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.Load();
            //Assess
            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }

        //Test case: exception while calling the Load Method
        [Fact]
        public void Load_WithException()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(ActiveSessionState.throws);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.Load();
            //Assess
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }


        //Test group: successfull execution of LoadAsync method
        [Fact]
        public void LoadAsync_Successful()
        {
            //Test case: the successfull first execution of LoadAsync method
            //Arrange
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup();
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.True(feature.IsLoaded);
            Assert.True(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);

            //Test case: successfull non-first executions of LoadAsync method (already Arranged)
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }

        //Test case: call LoadAsync method with an unavailable ISession
        [Fact]
        public void LoadAsync_UnavailableSession()
        {
            //Arrange
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(SessionState.unavailable);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Never);
        }

        //Test case: call LoadAsync method for a null ISession
        [Fact]
        public void LoadAsync_NullSession()
        {
            //Arrange
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(SessionState.absent);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.Null(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Never);
        }

        //Test case: call Load method leading to a null IActiveSession
        [Fact]
        public void LoadAsync_NullActiveSession()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(ActiveSessionState.isnull);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }

        //Test case: exception while calling the LoadAsync method
        [Fact]
        public void LoadAsync_WithException()
        {
            //Arrange
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(ActiveSessionState.throws);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionLoadExpression, Times.Once);
        }

        //Test group: call Clear method
        [Fact]
        public void Clear()
        {
            //Test case: clear not loaded feature
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup();
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            Assert.False(feature.IsLoaded);
            //Act
            feature.Clear();
            //Assess
            Assert.False(feature.IsLoaded);
            Assert.Null(feature.Session);
            Assert.False(feature.RawActiveSession.IsAvailable);

            //Test case: clear loaded feature
            //Arrange
            feature.Load();
            Assert.True(feature.IsLoaded);
            //Act
            feature.Clear();
            //Assess
            Assert.False(feature.IsLoaded);
            Assert.Null(feature.Session);
            Assert.False(feature.RawActiveSession.IsAvailable);
        }

        //Test group: getting statistics
        [Fact]
        public void GetCurrentStoreStatistics()
        {
            CurrentStoreStatisticsSetup test_setup;
            IActiveSessionFeature feature;
            ActiveSessionStoreStats? statistics;

            //Test case: no statistics tracking
            //Arrange
            test_setup=new CurrentStoreStatisticsSetup(false);
            feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            statistics = feature.GetCurrentStoreStatistics();
            //Assess
            Assert.Null(statistics);

            //Test case: statistics tracking active
            //Arrange
            test_setup=new CurrentStoreStatisticsSetup(true);
            feature=new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER);
            //Act
            statistics = feature.GetCurrentStoreStatistics();
            //Assess
            Assert.NotNull(statistics);
            Assert.Equal(1, statistics.SessionCount);
            Assert.Equal(2, statistics.RunnerCount);
            Assert.Equal(0, statistics.StoreSize);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Auxilary classes
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        enum SessionState { normal, absent, unavailable }

        class ConstructorTestSetup
        {
            public readonly Mock<IActiveSessionStore> MockStore;
            public readonly Mock<ISession>? MockSession;
            public readonly MockedLogger StubLogger;
            public ISession? SessionObject { get {return MockSession?.Object; } }

            public ConstructorTestSetup() : this(SessionState.normal) { }

            protected  ConstructorTestSetup(SessionState State)
            {
                MockStore=new Mock<IActiveSessionStore>();
                StubLogger=new MockedLogger(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
                if(State!=SessionState.absent) {
                    MockSession=new Mock<ISession>();
                    MockSession.SetupGet(s => s.IsAvailable).Returns(State==SessionState.normal);
                }
            }
        }

        const String TEST_SESSION_ID = "TEST_SESSION_ID";

        enum ActiveSessionState { normal, isnull, throws};
        
        class LoadTestSetup : ConstructorTestSetup
        {
            public readonly Mock<IActiveSession> StubActiveSession;
            public readonly Expression<Func<IActiveSessionStore, IActiveSession?>> ActiveSessionLoadExpression;

            public LoadTestSetup(ActiveSessionState ASState) : this(SessionState.normal, ASState) { }
            public LoadTestSetup() : this(SessionState.normal, ActiveSessionState.normal) { }
            public LoadTestSetup(SessionState State) : this(State, ActiveSessionState.normal) { }

            protected LoadTestSetup(SessionState State, ActiveSessionState ASState) : base(State)
            {
                StubActiveSession=new Mock<IActiveSession>();
                StubActiveSession.SetupGet(s => s.IsAvailable).Returns(true);
                ActiveSessionLoadExpression=s => s.FetchOrCreateSession(SessionObject!, It.IsAny<string>());
                switch(ASState) {
                    case ActiveSessionState.normal:
                        MockStore.Setup(ActiveSessionLoadExpression).Returns(StubActiveSession.Object);
                        break;
                    case ActiveSessionState.isnull:
                        MockStore.Setup(ActiveSessionLoadExpression).Returns((IActiveSession?)null);
                        break;
                    case ActiveSessionState.throws:
                        MockStore.Setup(ActiveSessionLoadExpression).Throws(new TestException());
                        break;
                }
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
            public LoadAsyncTestSetup(ActiveSessionState ASState) : this(SessionState.normal, ASState) { }
            public LoadAsyncTestSetup() : this(SessionState.normal, ActiveSessionState.normal) { }
            public LoadAsyncTestSetup(SessionState State) : this(State, ActiveSessionState.normal) { }

            protected LoadAsyncTestSetup(SessionState State, ActiveSessionState ASState) : base(State, ASState) 
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
