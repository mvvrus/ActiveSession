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
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER, null);
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
            IStoreActiveSessionItem active_session;

            //Test case: fill ActiveSession property, success
            //Arrange
            test_setup= new ActiveSessionTestSetup();
            feature = test_setup.CreateFeature();
            //Act
            active_session=(IStoreActiveSessionItem)feature.ActiveSession;
            //Assess
            Assert.True(active_session.IsAvailable);
            Assert.Equal(TEST_SESSION_ID, active_session.Id);
            Assert.Equal(2, test_setup.EnvProviderRefCount);
            test_setup.ResetEnvProviderRefCount();

            //Test case: fill ActiveSession property, whlie ActiveSession class instance cannot be created
            //Arrange
            test_setup=new ActiveSessionTestSetup(SessionState.unavailable);
            feature= test_setup.CreateFeature();
            //Act
            active_session=(IStoreActiveSessionItem)feature.ActiveSession;
            //Assess
            Assert.False(active_session.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }

        //Test group: Test CommitAsync method
        [Fact]
        public void CommitAsync()
        {
            //Test case: the ActiveSession instance is available
            //Arrange
            CommitAsyncTestSetup test_setup = new CommitAsyncTestSetup(SessionState.normal);
            ActiveSessionFeature feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER, null);
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
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER, null);
            feature.Load();
            //Act
            feature.CommitAsync().GetAwaiter().GetResult();
            //Asses
            test_setup.MockSession!.Verify(test_setup.SessionCommitAsyncExpression, Times.Never);

            //Test case: no session
            //Arrange
            test_setup=new CommitAsyncTestSetup(SessionState.absent);
            feature=new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER, null);
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
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.Load();
            //Assess
            Assert.True(feature.IsLoaded);
            Assert.True(feature.ActiveSession.IsAvailable);
            Assert.True(feature.LocalSession.IsAvailable);
            Assert.Equal(2, test_setup.EnvProviderRefCount);

            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Once);

            //Test case: successfull non-first executions of Load method (already Arranged)
            //Act
            feature.Load();
            //Assess
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Once);
            Assert.Equal(2, test_setup.EnvProviderRefCount);
        }

        //Test case: call Load method with an unavailable ISession
        [Fact]
        public void Load_UnavailableSession()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(SessionState.unavailable);
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.Load();
            //Assess
            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.NotNull(feature.ActiveSession);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Never);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }

        //Test case: call Load method with a null ISession
        [Fact]
        public void Load_NullSession()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(SessionState.absent);
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.Load();
            //Assess
            Assert.Null(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Never);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }

        //Test case: call Load method leading to a null IActiveSession
        [Fact]
        public void Load_NullActiveSession()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(ActiveSessionState.isnull);
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.Load();
            //Assess
            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Once);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }

        //Test case: exception while calling the Load Method
        [Fact]
        public void Load_WithException()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(ActiveSessionState.throws);
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.Load();
            //Assess
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Once);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }


        //Test group: successfull execution of LoadAsync method
        [Fact]
        public void LoadAsync_Successful()
        {
            //Test case: the successfull first execution of LoadAsync method
            //Arrange
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup();
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.True(feature.IsLoaded);
            Assert.True(feature.ActiveSession.IsAvailable);
            Assert.True(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Once);
            Assert.Equal(2, test_setup.EnvProviderRefCount);

            //Test case: successfull non-first executions of LoadAsync method (already Arranged)
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Once);
            Assert.Equal(2, test_setup.EnvProviderRefCount);
        }

        //Test case: call LoadAsync method with an unavailable ISession
        [Fact]
        public void LoadAsync_UnavailableSession()
        {
            //Arrange
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(SessionState.unavailable);
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Never);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }

        //Test case: call LoadAsync method for a null ISession
        [Fact]
        public void LoadAsync_NullSession()
        {
            //Arrange
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(SessionState.absent);
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.Null(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Never);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }

        //Test case: call Load method leading to a null IActiveSession
        [Fact]
        public void LoadAsync_NullActiveSession()
        {
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup(ActiveSessionState.isnull);
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.NotNull(feature.Session);
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Once);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }

        //Test case: exception while calling the LoadAsync method
        [Fact]
        public void LoadAsync_WithException()
        {
            //Arrange
            LoadAsyncTestSetup test_setup = new LoadAsyncTestSetup(ActiveSessionState.throws);
            ActiveSessionFeature feature = test_setup.CreateFeature();
            //Act
            feature.LoadAsync().GetAwaiter().GetResult();
            //Assess
            Assert.True(feature.IsLoaded);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSessionStoreFetchExpression, Times.Once);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
        }

        //Test group: call Clear method
        [Fact]
        public void Clear()
        {
            //Test case: clear not loaded feature
            //Arrange
            LoadTestSetup test_setup = new LoadTestSetup();
            ActiveSessionFeature feature = test_setup.CreateFeature();
            Assert.False(feature.IsLoaded);
            //Act
            feature.Clear();
            //Assess
            Assert.False(feature.IsLoaded);
            Assert.Null(feature.Session);
            Assert.False(feature.RawActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSesionStoreDetachExpression, Times.Never);
            Assert.Equal(0, test_setup.EnvProviderRefCount);

            //Test case: clear loaded feature
            //Arrange
            feature = test_setup.CreateFeature();
            feature.Load();
            Assert.True(feature.IsLoaded);
            //Act
            feature.Clear();
            //Assess
            Assert.False(feature.IsLoaded);
            Assert.Null(feature.Session);
            Assert.False(feature.RawActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSesionStoreDetachExpression, Times.Once);
            Assert.Equal(1, test_setup.EnvProviderRefCount);
            test_setup.SimulateEviction();
            Assert.Equal(0, test_setup.EnvProviderRefCount);

            //Test case:clear unavailable feature
            test_setup=new LoadTestSetup(SessionState.unavailable);
            //Arrange
            feature.Load();
            Assert.True(feature.IsLoaded);
            //Act
            feature.Clear();
            //Assess
            Assert.False(feature.IsLoaded);
            Assert.Null(feature.Session); 
            Assert.False(feature.RawActiveSession.IsAvailable);
            test_setup.MockStore.Verify(test_setup.ActiveSesionStoreDetachExpression, Times.Never);
            Assert.Equal(0, test_setup.EnvProviderRefCount);
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
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER, null);
            //Act
            statistics = feature.GetCurrentStoreStatistics();
            //Assess
            Assert.Null(statistics);

            //Test case: statistics tracking active
            //Arrange
            test_setup=new CurrentStoreStatisticsSetup(true);
            feature=new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER, null);
            //Act
            statistics = feature.GetCurrentStoreStatistics();
            //Assess
            Assert.NotNull(statistics);
            Assert.Equal(1, statistics.SessionCount);
            Assert.Equal(2, statistics.RunnerCount);
            Assert.Equal(0, statistics.StoreSize);
        }

        //Test case: ActiveSession id with suffix 
        [Fact]
        public void ActiveSessionWithSuffix()
        {
            const string TEST_SUFFIX = "TestSuffix";
            ActiveSessionTestSetup test_setup;
            ActiveSessionFeature feature;
            IStoreActiveSessionItem active_session;

            //Test case: fill ActiveSession property, success
            //Arrange
            test_setup= new ActiveSessionTestSetup();
            feature = new ActiveSessionFeature(
                test_setup.MockStore.Object, test_setup.SessionObject, test_setup.StubLogger.Logger, TEST_TRACE_IDENTIFIER, TEST_SUFFIX);
            //Act
            active_session=(IStoreActiveSessionItem)feature.ActiveSession;
            //Assess
            Assert.True(active_session.IsAvailable);
            Assert.True(feature.LocalSession.IsAvailable);
            Assert.Equal(MakeId(TEST_SUFFIX), active_session.Id);
            Assert.Equal(TEST_SESSION_ID, feature.LocalSession.Id);
            Assert.Equal(2, test_setup.EnvProviderRefCount);
            test_setup.ResetEnvProviderRefCount();
        }

        //Test group: RefreshActiveSession method
        [Fact]
        public void RefreshActiveSession()
        {
            IActiveSessionFeature feature;
            Boolean result;
            IActiveSession old_as;
            RefreshTestSetup ts;

            //Test case: !ActiveSession.IsLoaded 
            ts =new RefreshTestSetup();
            feature = ts.CreateFeature();
            result=feature.RefreshActiveSession();
            Assert.False(result);

            //Test case: !ActiveSession.IsAvalable
            ts = new RefreshTestSetup(SessionState.unavailable);
            feature = ts.CreateFeature();
            old_as=feature.ActiveSession;
            result=feature.RefreshActiveSession();
            Assert.False(result);
            ts.MockStore.Verify(ts.ActiveSessionStoreFetchExpression, Times.Never);
            Assert.Equal(0, ts.EnvProviderRefCount);

            //Test case: refreshed ActiveSession is the same
            ts = new RefreshTestSetup();
            feature = ts.CreateFeature();
            old_as=feature.ActiveSession;
            result=feature.RefreshActiveSession();
            Assert.False(result);
            Assert.Same(old_as, feature.ActiveSession);
            ts.MockStore.Verify(ts.ActiveSessionStoreFetchExpression, Times.Exactly(2));
            Assert.Equal(2, ts.EnvProviderRefCount);

            //Test case: store returns null while refreshing
            ts = new RefreshTestSetup();
            feature = ts.CreateFeature();
            old_as=feature.ActiveSession;
            ts.SwitchActiveSession(false);
            ts.MockStore.Verify(ts.ActiveSessionStoreFetchExpression, Times.Once);
            result=feature.RefreshActiveSession();
            Assert.True(result);
            Assert.False(feature.ActiveSession.IsAvailable);
            Assert.False(feature.LocalSession.IsAvailable);
            ts.MockStore.Verify(ts.ActiveSessionStoreFetchExpression, Times.Exactly(2));
            Assert.Equal(1, ts.EnvProviderRefCount); 

            //Test case: refreshed ActiveSession is not the same, available
            ts = new RefreshTestSetup();
            feature = ts.CreateFeature();
            old_as=feature.ActiveSession;
            ts.SwitchActiveSession();
            result=feature.RefreshActiveSession();
            Assert.True(result);
            Assert.NotSame(old_as, feature.ActiveSession);
            Assert.True(feature.ActiveSession.IsAvailable);
            Assert.True(feature.LocalSession.IsAvailable);
            ts.MockStore.Verify(ts.ActiveSessionStoreFetchExpression, Times.Exactly(2));
            Assert.Equal(2, ts.EnvProviderRefCount);
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Auxilary classes
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        static String MakeId(String? Suffix)
        {
            return TEST_SESSION_ID+(String.IsNullOrEmpty(Suffix) ? "" : "-"+Suffix);
        }

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

        class ActiveSessionTestSetup: ConstructorTestSetup
        {
            public readonly Mock<IStoreActiveSessionItem> StubActiveSession;
            public readonly Mock<IStoreGroupItem> StubBaseGroup;
            Int32 _envProviderRefCount = 0;
            protected IStoreActiveSessionItem? _activeSessionItemToReturn = null;
            Boolean _inCache = false;
            String? _suffix = null;

            public String? Suffix => _suffix;
            public Int32 EnvProviderRefCount => _envProviderRefCount;
            public Boolean RefMustExist { get; set; } = false;
            public readonly Expression<Func<IActiveSessionStore, IStoreActiveSessionItem?>> ActiveSessionStoreFetchExpression;
            public readonly Expression<Action<IActiveSessionStore>> ActiveSesionStoreDetachExpression;
            protected String GetId() => MakeId(Suffix);


            public ActiveSessionTestSetup() : this(SessionState.normal) { }

            public ActiveSessionTestSetup(SessionState State): base(State)
            {
                ActiveSessionStoreFetchExpression=s => s.FetchOrCreateSession(SessionObject!, It.IsAny<string>(), It.IsAny<String?>());
                ActiveSesionStoreDetachExpression = s => s.DetachSession(It.IsAny<ISession>(), It.IsAny<IStoreActiveSessionItem>(), It.IsAny<String?>());
                StubBaseGroup = new Mock<IStoreGroupItem>();
                StubBaseGroup.SetupGet(s => s.IsAvailable).Returns(true);
                StubBaseGroup.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                StubActiveSession =new Mock<IStoreActiveSessionItem>();
                StubActiveSession.SetupGet(s => s.IsAvailable).Returns(true);
                StubActiveSession.SetupGet(s => s.Id).Returns(GetId);
                StubActiveSession.SetupGet(s => s.BaseGroup).Returns(StubBaseGroup.Object);
                MockStore.Setup(ActiveSessionStoreFetchExpression)
                    .Callback(FetchOrCreateSessionCallback)
                    .Returns(FetchOrCreateSessionResults);
                MockStore.Setup(ActiveSesionStoreDetachExpression)
                    .Callback(DetachSessionCallback);
            }

            public ActiveSessionFeature CreateFeature()
            {
                return new ActiveSessionFeature(MockStore.Object, SessionObject, StubLogger.Logger, TEST_TRACE_IDENTIFIER, null);
            }

            public void SimulateEviction()
            {
                _inCache=false;
                DecRefCount();
            }

            public void ResetEnvProviderRefCount()
            {
                _inCache=false;
                _envProviderRefCount=0;
            }

            protected virtual void FetchOrCreateSessionCallback(ISession Session, String? TraceIdentifier, String Suffix)
            {
                _suffix=Suffix;
                if(!_inCache) IncRefCount();
                _inCache = true;
                _activeSessionItemToReturn = StubActiveSession.Object;
            }

            protected virtual IStoreActiveSessionItem? FetchOrCreateSessionResults()
            {
                if(_activeSessionItemToReturn!=null) IncRefCount();
                return _activeSessionItemToReturn;
            }

            protected virtual void DetachSessionCallback(ISession Session, IStoreActiveSessionItem ActiveSessionItem, String? TraceIdentifier)
            {
                if(ActiveSessionItem?.IsAvailable??false) DecRefCount();
            }

            protected void IncRefCount()
            {
                _envProviderRefCount++;
            }

            protected void DecRefCount()
            {
                if(_envProviderRefCount>0) {
                    if(--_envProviderRefCount==0 && RefMustExist) throw new InvalidOperationException("Rerfernce may not fall to 0.");
                }
                else throw new InvalidOperationException("Excessive  refcount decrement.");
            }

        }

        class RefreshTestSetup : ActiveSessionTestSetup
        {
            Action<CancellationToken>? _loadAsyncCallback = null;
            readonly Mock<IStoreActiveSessionItem> _asMock;
            static readonly Func<Task> s_defaultLoadAsyncResultTask = () => Task.CompletedTask;
            Func<Task> _loadAsyncResultTask = s_defaultLoadAsyncResultTask;
            Boolean _changeSession=false;
            Boolean _makeAvailable;

            public RefreshTestSetup() : this(SessionState.normal) { }

            public RefreshTestSetup(SessionState State) : base(State) 
            {
                _asMock = new Mock<IStoreActiveSessionItem>();
                _asMock.SetupGet(s => s.IsAvailable).Returns(true);
                _asMock.SetupGet(s => s.Id).Returns(GetId);
                _asMock.SetupGet(s => s.BaseGroup).Returns(StubBaseGroup.Object);
                MockSession!.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
                    .Callback((CancellationToken t) => { _loadAsyncCallback?.Invoke(t); t.ThrowIfCancellationRequested(); })
                    .Returns(() => _loadAsyncResultTask());
            }

            protected override void FetchOrCreateSessionCallback(ISession Session, String? TraceIdentifier, String Suffix)
            {
                base.FetchOrCreateSessionCallback(Session,TraceIdentifier, Suffix);
                if(_changeSession) _activeSessionItemToReturn = _makeAvailable?_asMock.Object:null;
            }

            public void SwitchActiveSession(Boolean MakeAvailable=true)
            {
                if(_changeSession) throw new InvalidOperationException("Cannot switch active session twice.");
                SimulateEviction();
                _changeSession=true;
                _makeAvailable=MakeAvailable;
            }

            internal void SetLoadAsyncCallback(Action<CancellationToken>? Callback)
            {
                _loadAsyncCallback=Callback;
                _loadAsyncResultTask = s_defaultLoadAsyncResultTask;
            }

            internal void SetLoadAsyncCallback(Func<CancellationToken, Task> Callback)
            {
                _loadAsyncCallback=(CancellationToken token) => { _loadAsyncResultTask = () => Callback(token); };
            }
        }

        const String TEST_SESSION_ID = "TEST_SESSION_ID";

        enum ActiveSessionState { normal, isnull, throws};

        class LoadTestSetup : ActiveSessionTestSetup
        {

            ActiveSessionState _aSState;
            public LoadTestSetup(ActiveSessionState ASState) : this(SessionState.normal, ASState) { }
            public LoadTestSetup() : this(SessionState.normal, ActiveSessionState.normal) { }
            public LoadTestSetup(SessionState State) : this(State, ActiveSessionState.normal) { }

            protected LoadTestSetup(SessionState State, ActiveSessionState ASState) : base(State)
            {
                _aSState=ASState;
            }

            protected override void FetchOrCreateSessionCallback(ISession Session, String? TraceIdentifier, String Suffix)
            {
                switch(_aSState) {
                    case ActiveSessionState.normal:
                        base.FetchOrCreateSessionCallback(Session, TraceIdentifier, Suffix);
                        break;
                    case ActiveSessionState.isnull:
                        _activeSessionItemToReturn=null;
                        break;
                    case ActiveSessionState.throws:
                        break;
                }
            }

            protected override IStoreActiveSessionItem? FetchOrCreateSessionResults()
            {
                if(_aSState==ActiveSessionState.throws) throw new TestException();
                else return base.FetchOrCreateSessionResults();
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
