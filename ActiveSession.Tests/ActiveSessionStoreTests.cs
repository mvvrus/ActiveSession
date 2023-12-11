using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Linq.Expressions;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using Active_Session = MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;


namespace ActiveSession.Tests
{
    public class ActiveSessionStoreTests
    {
        const String TEST_ARG1 = "TEST_ARG1";
        static readonly TimeSpan s_defaultIdleTimeout = TimeSpan.FromMinutes(20);

        [Fact]
        public void CreateActiveSessionStore()
        {
            ConstructorTestSetup ts;
            Mock<IMemoryCache> dummy_cache = new Mock<IMemoryCache>();
            ts=new ConstructorTestSetup(dummy_cache);

            //Test case: null IMemoryCahe argument while own caches is not used
            Assert.Throws<InvalidOperationException>(() => new ActiveSessionStore(
                null, ts.RootSP, ts.StubRMFactory.Object, ts.IActSessionOptions, ts.ISessOptions));

            //Test case: null IServiceProvider argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, null!, ts.StubRMFactory.Object, ts.IActSessionOptions, ts.ISessOptions));

            //Test case: null IRunnerManagerFactory argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, ts.RootSP, null!, ts.IActSessionOptions, ts.ISessOptions));

            //Test case: null IOptions<ActiveSessionOptions> argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, ts.RootSP, ts.StubRMFactory.Object, null!, ts.ISessOptions));

            //Test case: null IOptions<SessionOptions> argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, ts.RootSP, ts.StubRMFactory.Object, ts.IActSessionOptions, null!));

            //Test case: using shared cache
            using (ts.CreateStore()) {
                Assert.True(ts.LoggerCreated());
            }
        }

        [Fact]
        public void GetCurrentStatistics()
        {
            ConstructorTestSetup ts;
            Mock<IMemoryCache> dummy_cache = new Mock<IMemoryCache>();
            ActiveSessionStore store;
            ActiveSessionStoreStats? stats;
            ts=new ConstructorTestSetup(dummy_cache);

            //Test case: no statistics tracking
            using (store=ts.CreateStore()) {
                stats=store.GetCurrentStatistics();

                Assert.Null(stats);
            }

            //Test case: statistics tracking active, store just created
            ts.ActSessOptions.TrackStatistics=true;
            using (store=ts.CreateStore()) {
                stats=store.GetCurrentStatistics();

                Assert.NotNull(stats);
                Assert.Equal(0, stats.SessionCount);
                Assert.Equal(0, stats.RunnerCount);
                Assert.Equal(0, stats.StoreSize);
            }
        }

        [Fact]
        public void CreateOrFetchSession()
        {
            CreateFetchTestSetup ts;
            ActiveSessionStore store;
            IActiveSession? session;
            IRunnerManager? manager;

            using (ts=new CreateFetchTestSetup()) {
                using (store=ts.CreateStore()) {

                    //Test case: create new ActiveSession
                    session=store.FetchOrCreateSession(ts.StubSession.Object, null);

                    //Assess
                    Assert.NotNull(session);
                    //Assess IActiveSession
                    Assert.Equal(CreateFetchTestSetup.TEST_SESSION_ID, session.Id);
                    Assert.Equal(ts.ScopeServiceProvider, session.SessionServices);
                    Assert.True(session.CompletionToken.CanBeCanceled);
                    Assert.False(session.CompletionToken.IsCancellationRequested);
                    //Assess IRunnerManager
                    manager=(session as Active_Session)?.RunnerManager;
                    Assert.NotNull(manager);
                    Assert.NotNull(manager.RunnerCreationLock);
                    ts.MockRunnerManager.Verify(ts.RegisterSessionExpression, Times.Once);
                    //Assess a cache entry
                    ts.Cache.CacheMock.Verify(MockedCache.TryGetValueExpression, Times.Exactly(2));//2-nd time - after obtainning lock. Fragile!!! 
                    ts.Cache.CacheMock.Verify(MockedCache.CreateEntryEnpression, Times.Once);
                    Assert.True(ts.Cache.IsEntryStored);
                    Assert.Equal(DEFAULT_SESSION_KEY_PREFIX+"_"+CreateFetchTestSetup.TEST_SESSION_ID, ts.Cache.Key);
                    Assert.True(ReferenceEquals(session, ts.Cache.Value));
                    Assert.Equal(s_defaultIdleTimeout, ts.Cache.SlidingExpiration);
                    Assert.Equal(DEFAULT_MAX_LIFETIME, ts.Cache.AbsoluteExpirationRelativeToNow);
                    Assert.Null(ts.Cache.AbsoluteExpiration);
                    Assert.Equal(CacheItemPriority.Normal, ts.Cache.Priority);
                    Assert.Equal(1, ts.Cache.ExpirationTokens.Count);
                    Assert.True(ts.Cache.ExpirationTokens[0].ActiveChangeCallbacks);
                    Assert.False(ts.Cache.ExpirationTokens[0].HasChanged);
                    Assert.Equal(1, ts.Cache.PostEvictionCallbacks.Count);

                    //Test case: fetch ActiveSession from cache
                    IActiveSession? session2 = store.FetchOrCreateSession(ts.StubSession.Object, null);
                    ts.Cache.CacheMock.Verify(MockedCache.TryGetValueExpression, Times.Exactly(3));
                    ts.Cache.CacheMock.Verify(MockedCache.CreateEntryEnpression, Times.Once);
                    Assert.True(Object.ReferenceEquals(session, session2));
                    //Test case: disposing ActiveSession while in a cache object w/o runners associated
                    //Arrange more
                    IServiceProvider session_sp = session.SessionServices;
                    //Act
                    (session as IDisposable)?.Dispose();
                    //Assess
                    Assert.False(ts.Cache.IsEntryStored);
                    Assert.Equal(1, ts.Cache.CalledCallbacksCount);
                    Assert.True((session as Active_Session)!.Disposed);
                    Task cleanup_task = session.CleanupCompletionTask;
                    Assert.NotEqual(TaskStatus.Created, cleanup_task.Status);
                    cleanup_task.GetAwaiter().GetResult();
                    ts.MockRunnerManager.Verify(ts.AbortAllExpression, Times.Once);
                    ts.MockRunnerManager.Verify(ts.PerformRunnersCleanupExpression, Times.Once);
                    Assert.Throws<ObjectDisposedException>(()=>session_sp.GetService(typeof(ILoggerFactory)));
                }

                //Test case: options passed to ActiveSessionStore constructor affects cache entry
                TimeSpan EXPIRATION = TimeSpan.FromMinutes(1);
                TimeSpan MAX_LIFETIME = TimeSpan.FromHours(1);
                String PREFIX = "TestPrefix";
                Int32 AS_SIZE = 2;
                //Arrange
                ts.SessOptions.IdleTimeout=EXPIRATION;
                ts.ActSessOptions.MaxLifetime=MAX_LIFETIME;
                ts.ActSessOptions.Prefix=PREFIX;
                ts.ActSessOptions.TrackStatistics=true;
                ts.ActSessOptions.ActiveSessionSize=AS_SIZE;
                using (store=ts.CreateStore()) {
                    session=store.FetchOrCreateSession(ts.StubSession.Object, null);
                    //Assess
                    Assert.NotNull(session);
                    Assert.True(ts.Cache.IsEntryStored);
                    Assert.Equal(PREFIX+"_"+CreateFetchTestSetup.TEST_SESSION_ID, ts.Cache.Key);
                    Assert.Equal(EXPIRATION, ts.Cache.SlidingExpiration);
                    Assert.Equal(MAX_LIFETIME, ts.Cache.AbsoluteExpirationRelativeToNow);
                    Assert.Equal(AS_SIZE, store.GetCurrentStatistics()!.StoreSize);

                    //Test case: removing ActiveSession from cache. 
                    //Arrange more
                    IServiceProvider session_sp = session.SessionServices;
                    //Act
                    ts.Cache.CacheMock.Object.Remove(PREFIX+"_"+CreateFetchTestSetup.TEST_SESSION_ID);
                    //Assess
                    Assert.Equal(0, store.GetCurrentStatistics()!.StoreSize);
                    Assert.False(ts.Cache.IsEntryStored);
                    Assert.Equal(1, ts.Cache.CalledCallbacksCount);
                    Assert.True((session as Active_Session)!.Disposed);
                    Task cleanup_task = session.CleanupCompletionTask;
                    Assert.NotEqual(TaskStatus.Created, cleanup_task.Status);
                    cleanup_task.GetAwaiter().GetResult();
                    //Verify the next two calls are made once, accounting for 1 call made by an earlier test
                    ts.MockRunnerManager.Verify(ts.AbortAllExpression, Times.Exactly(2)); 
                    ts.MockRunnerManager.Verify(ts.PerformRunnersCleanupExpression, Times.Exactly(2));
                    Assert.Throws<ObjectDisposedException>(() => session_sp.GetService(typeof(ILoggerFactory)));

                    //TODO? Test case: disposing ActiveSession while in the cache w/runners associated
                    //TODO Test case: logging timely runners cleanup after eviction
                }
            }
        }

        [Fact]
        public void CreateOrFetchSession_Race()
        {
            //Test case: race condition in FetchOrCreateSession method
            CreateFetchTestSetup ts;
            ActiveSessionStore store;
            IActiveSession? session1, session2;
            Task<IActiveSession> task1, task2;
            ManualResetEvent proceed_event = null!, event1 = null!, event2 = null!;
            Int32 pause_count = 0;
            Action pause = () => { proceed_event.WaitOne(); Interlocked.Increment(ref pause_count); };

            //Arrange
            using (ts=new CreateFetchTestSetup()) {
                try {
                    proceed_event=new ManualResetEvent(false);
                    event1=new ManualResetEvent(false);
                    event2=new ManualResetEvent(false);

                    ts.Cache.CreateEntryTrap=pause;
                    using (store=ts.CreateStore()) {
                        //Act
                        task1=Task.Run(() => { event1.Set(); return store.FetchOrCreateSession(ts.StubSession.Object, null); });
                        if (!event1.WaitOne(2000))
                            throw new Exception("Deadlock detected");
                        task2=Task.Run(() => { event2.Set(); return store.FetchOrCreateSession(ts.StubSession.Object, null); });
                        if (!event2.WaitOne(2000))
                            throw new Exception("Deadlock detected");
                        Assert.Equal(TaskStatus.Running, task1.Status);
                        Assert.Equal(TaskStatus.Running, task2.Status);
                        proceed_event.Set();
                        session1=task1.GetAwaiter().GetResult();
                        session2=task2.GetAwaiter().GetResult();
                        //Assess
                        Assert.NotNull(task1);
                        Assert.True(ReferenceEquals(session1, session2));
                        Assert.Equal(1, pause_count);
                        ts.Cache.CacheMock.Verify(MockedCache.CreateEntryEnpression, Times.Once);
                        Assert.True(ts.Cache.IsEntryStored);
                        Assert.Equal(DEFAULT_SESSION_KEY_PREFIX+"_"+CreateFetchTestSetup.TEST_SESSION_ID, ts.Cache.Key);
                    }

                }
                finally {
                    proceed_event?.Dispose();
                    event1.Dispose();
                    event2.Dispose();
                }
            }
        }

        //Test group: test normal flow around CreatRunner method called for an ActiveSessinonStore object created with default options
        [Fact]
        public void CreateRunner()
        {
            RunnerTestSetup ts;
            ActiveSessionStore store;
            KeyedActiveSessionRunner<Result1> runner_and_key;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts=null!;  //Inialize to avoid false error concerning use of an uninitialized variable

            //Test case: create new runner with ActiveSession level lock and default options
            //Arrange
            using (ts=new RunnerTestSetup()) {
                ts.AddRunnerFactory<Request1, Result1>(
                    arg => {
                        dummy_runner1=new MockedRunner<Request1, Result1>(cts, arg);
                        return dummy_runner1.Runner;
                    }
                );
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        //Act
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Assess
                        //Check method result
                        Assert.Equal(dummy_runner1?.Runner, runner_and_key.Runner);
                        Assert.Equal(RunnerTestSetup.RUNNER_1, runner_and_key.RunnerNumber);
                        Assert.Equal(TEST_ARG1, dummy_runner1?.Arg.Arg);
                        //Check runner keys added to ISession
                        String runner_key = DEFAULT_SESSION_KEY_PREFIX+"_"+RunnerTestSetup.TEST_SESSION_ID
                            +"_"+RunnerTestSetup.RUNNER_1.ToString();
                        Assert.Equal(DEFAULT_HOST_NAME, ts.MockSession.Object.GetString(runner_key));
                        Assert.Equal(typeof(Result1).FullName, ts.MockSession.Object.GetString(runner_key+"_Type"));
                        //Check cache entry
                        ts.Cache.CacheMock.Verify(MockedCache.CreateEntryEnpression, Times.Once);
                        Assert.True(ts.Cache.IsEntryStored);
                        Assert.Equal(runner_key, ts.Cache.Key);
                        Assert.True(ReferenceEquals(runner_and_key.Runner, ts.Cache.Value));
                        Assert.Equal(s_defaultIdleTimeout, ts.Cache.SlidingExpiration);
                        Assert.Equal(DEFAULT_MAX_LIFETIME, ts.Cache.AbsoluteExpirationRelativeToNow);
                        Assert.Null(ts.Cache.AbsoluteExpiration);
                        Assert.Equal(CacheItemPriority.Normal, ts.Cache.Priority);
                        Assert.Equal(1, ts.Cache.ExpirationTokens.Count);
                        Assert.True(ts.Cache.ExpirationTokens[0].ActiveChangeCallbacks);
                        Assert.False(ts.Cache.ExpirationTokens[0].HasChanged);
                        Assert.Equal(1, ts.Cache.PostEvictionCallbacks.Count);
                        //Check runner manager calls
                        ts.MockRunnerManager.Verify(ts.RegisterRunnerExpression, Times.Once);
                        ts.MockRunnerManager.Verify(ts.ReturnRunnerNumberExpression, Times.Never);
                        ts.MockRunnerManager.Verify(ts.UnregisterRunnerExpression, Times.Never);

                        //Test case: Evict the runner from the cache (via Remove)
                        // and check execution of the eviction callback
                        //Act
                        ts.Cache.CacheMock.Object.Remove(runner_key);
                        //Assess
                        //Check that cache entry was evicted
                        Assert.False(ts.Cache.IsEntryStored);
                        //Check that the runner was unregistered
                        ts.MockRunnerManager.Verify(ts.RegisterRunnerExpression, Times.Once);
                        ts.MockRunnerManager.Verify(ts.ReturnRunnerNumberExpression, Times.Once);
                        ts.MockRunnerManager.Verify(ts.UnregisterRunnerExpression, Times.Once);
                        //Check that ISesson variables are left intact
                        Assert.Equal(DEFAULT_HOST_NAME, ts.MockSession.Object.GetString(runner_key));
                        Assert.Equal(typeof(Result1).FullName, ts.MockSession.Object.GetString(runner_key+"_Type"));
                    }
                    //Test case: get runner factory from cache
                    //Arrange
                    using (cts=new CancellationTokenSource()) {
                        //Act
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Assess
                        //Check method result
                        Assert.Equal(dummy_runner1?.Runner, runner_and_key.Runner);
                        Assert.Equal(RunnerTestSetup.RUNNER_1, runner_and_key.RunnerNumber);
                        Assert.Equal(TEST_ARG1, dummy_runner1?.Arg.Arg);
                        //Check check number of searches of the runner factory
                        ts.MockRootServiceProvider.Verify(ts.RunnerFactoryExpression<Request1, Result1>(), Times.Once);
                    }

                }
                //Test case: try to create new runner while the store is disposed.
                //Aready arranged. Act and assess.
                using (cts=new CancellationTokenSource()) {
                    Assert.Throws<ObjectDisposedException>(
                    () => {
                            runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        }
                    );
                }

            }
        }

        //Test group: test clanup procedures used when an exception occurs during a call of a CreateRunner method 
        [Fact]
        public void CreateRunner_WithExceptions()
        {
            //Test case: attempt to create new runner with exceptions occured, 4 stages to test 4 possible remedy actions
            RunnerTestSetup ts;
            ActiveSessionStore store;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts;

            using (ts=new RunnerTestSetup()) {
                //Test case: create new runner with ActiveSession level lock and default options
                //Arrange
                Action? stage2_callback = () => { throw new TestException2(); };
                ts.CreateStage1Callback=() => { throw new TestException1(); };
                ts.CreateStage3Callback=() => { throw new TestException3(); };
                ts.CreateStage4Callback=() => { throw new TestException4(); };
                using (cts=new CancellationTokenSource()) {
                    ts.AddRunnerFactory<Request1, Result1>(
                        arg => {
                            stage2_callback?.Invoke();
                            return (dummy_runner1=new MockedRunner<Request1, Result1>(cts, arg)).Runner; 
                        }
                    );
                    using (store=ts.CreateStore()) {
                        //Act & assess stage 1
                        Assert.Throws<TestException1>(
                            ()=>store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                                ts.StubActiveSession.Object,
                                ts.MockRunnerManager.Object,
                                request,
                            null)
                        );
                        Assert.False(Monitor.IsEntered(ts.LockObject));
                        //Arrange stage 2
                        ts.CreateStage1Callback=null;
                        //Act & assess stage 2
                        Assert.Throws<TestException2>(
                            () => store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                                ts.StubActiveSession.Object,
                                ts.MockRunnerManager.Object,
                                request,
                            null)
                        );
                        Assert.False(Monitor.IsEntered(ts.LockObject));
                        Assert.False(ts.Cache.IsEntryStored);
                        //Arrange stage 3
                        stage2_callback=null;
                        //Act & assess stage 3
                        Assert.Throws<TestException3>(
                            () => store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                                ts.StubActiveSession.Object,
                                ts.MockRunnerManager.Object,
                                request,
                            null)
                        );
                        Assert.False(Monitor.IsEntered(ts.LockObject));
                        Assert.False(ts.Cache.IsEntryStored);
                        ts.MockSession.Verify(ts.SessionKeyRemoveExpression, Times.Never);
                        //Arrange stage 4
                        ts.CreateStage3Callback=null;
                        //Act & assess stage 4
                        Assert.Throws<TestException4>(
                            () => store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                                ts.StubActiveSession.Object,
                                ts.MockRunnerManager.Object,
                                request,
                            null)
                        );
                        Assert.False(Monitor.IsEntered(ts.LockObject));
                        Assert.False(ts.Cache.IsEntryStored);
                        ts.MockSession.Verify(ts.SessionKeyRemoveExpression, Times.AtLeast(2)); //Two keys for runner was added and should be removed
                    }
                }
            }
        }

        //Test group: call CreatRunner method for an ActiveSessinonStore object created with non-default options
        [Fact]
        public void CreateRunner_NonDefaultOptions()
        {
            RunnerTestSetup ts;
            ActiveSessionStore store;
            KeyedActiveSessionRunner<Result1> runner_and_key;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts = null!;  //Inialize to avoid false error concerning use of an uninitialized variable

            //Test case: create new runner with store level lock and custom options
            TimeSpan EXPIRATION = TimeSpan.FromMinutes(1);
            TimeSpan MAX_LIFETIME = TimeSpan.FromHours(1);
            String PREFIX = "TestPrefix";
            Int32 ASR_SIZE = 10;
            //Arrange
            using (ts=new RunnerTestSetup()) {
                ts.SessOptions.IdleTimeout=EXPIRATION;
                ts.ActSessOptions.MaxLifetime=MAX_LIFETIME;
                ts.ActSessOptions.Prefix=PREFIX;
                ts.ActSessOptions.TrackStatistics=true;
                ts.ActSessOptions.CacheRunnerAsTask=true;
                ts.ActSessOptions.DefaultRunnerSize=ASR_SIZE;
                ts.AddRunnerFactory<Request1, Result1>(
                    arg => {
                        dummy_runner1=new MockedRunner<Request1, Result1>(cts, arg);
                        return dummy_runner1.Runner;
                    }
                );

                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        //Act
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Assess
                        //Check method result
                        Assert.Equal(dummy_runner1?.Runner, runner_and_key.Runner);
                        Assert.Equal(RunnerTestSetup.RUNNER_1, runner_and_key.RunnerNumber);
                        Assert.Equal(TEST_ARG1, dummy_runner1?.Arg.Arg);
                        //Check cache entry (TODO)
                        ts.Cache.CacheMock.Verify(MockedCache.CreateEntryEnpression, Times.Once);
                        Assert.True(ts.Cache.IsEntryStored);
                        String runner_key = PREFIX+"_"+RunnerTestSetup.TEST_SESSION_ID
                            +"_"+RunnerTestSetup.RUNNER_1.ToString();
                        Assert.IsType<Task<IActiveSessionRunner<Result1>>>(ts.Cache.Value);
                        Assert.True(ReferenceEquals(runner_and_key.Runner, ((Task<IActiveSessionRunner<Result1>>)(ts.Cache.Value)).Result));
                        Assert.Equal(runner_key, ts.Cache.Key);
                        Assert.Equal(EXPIRATION, ts.Cache.SlidingExpiration);
                        Assert.Equal(MAX_LIFETIME, ts.Cache.AbsoluteExpirationRelativeToNow);
                        Assert.Equal(ASR_SIZE, store.GetCurrentStatistics()!.StoreSize);

                        //Test case: Evict the runner from the cache (via Remove) and check StoreSize (already arranged)
                        //Act
                        ts.Cache.CacheMock.Object.Remove(runner_key);
                        //Assess
                        Assert.Equal(0, store.GetCurrentStatistics()!.StoreSize);
                        Assert.False(ts.Cache.IsEntryStored);
                        Assert.Equal(1, ts.Cache.CalledCallbacksCount);
                    }
                }
            }
        }

        //Test group: GetRunner method call
        [Fact]
        public void GetRunner()
        {
            RunnerTestSetup ts;
            ActiveSessionStore store;
            KeyedActiveSessionRunner<Result1> runner_and_key;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts = null!;  //Inialize to avoid false error concerning use of an uninitialized variable
            IActiveSessionRunner<Result1>? runner;

            //Test case: search for an existing runner
            //Arrange
            using (ts=new RunnerTestSetup()) {
                ts.AddRunnerFactory<Request1, Result1>(
                    arg => {
                        dummy_runner1=new MockedRunner<Request1, Result1>(cts, arg);
                        return dummy_runner1.Runner;
                    }
                );
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Act
                        runner=store.GetRunner<Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null);
                        //Assess
                        Assert.True(ReferenceEquals(runner_and_key.Runner, runner));

                        //Test case: search for a non-existing runner, no associated session values (already arranged)
                        //Act
                        runner=store.GetRunner<Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber+1,
                            null);
                        //Assess
                        Assert.Null(runner);
                        //Test case: search for an already removed runner
                        //Arrange:Evict the runner from the cache (via Remove)
                        String runner_key = DEFAULT_SESSION_KEY_PREFIX+"_"+ts.MockSession.Object.Id+"_"+runner_and_key.RunnerNumber.ToString();
                        ts.Cache.CacheMock.Object.Remove(runner_key);
                        //Act
                        runner=store.GetRunner<Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null);
                        //Assess
                        Assert.Null(runner);
                        //Check that ISesson variables have been cleared
                        Assert.Null(ts.MockSession.Object.GetString(runner_key));
                        Assert.Null(ts.MockSession.Object.GetString(runner_key+"_Type"));
                    }
                }

                //Test case: try to get a runner from the disposed store (Already arranged)
                //Act & assess
                Assert.Throws<ObjectDisposedException>(
                        ()=> {
                            store.GetRunner<Result1>(ts.MockSession.Object,
                                ts.StubActiveSession.Object,
                                ts.MockRunnerManager.Object,
                                runner_and_key.RunnerNumber,
                                null);
                            }
                        );

                const int UNASSIGNABLE_TYPE_WARNING_ID = 1160;
                //Test case: search for an existing runner with incompatible type
                //Arrange
                MockedLogger logger_mock = ts.InitLogger();
                logger_mock.MonitorLogEntry(LogLevel.Warning, UNASSIGNABLE_TYPE_WARNING_ID);
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Act
                        IActiveSessionRunner<String>? runner2 =store.GetRunner<String>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null);
                        //Assess
                        Assert.Null(runner2);
                        logger_mock.VerifyLogEntry(LogLevel.Warning, UNASSIGNABLE_TYPE_WARNING_ID, Times.Once()); 
                    }
                }
                logger_mock= ts.InitLogger();

                //Test case: search for an existing runner, cached as task
                //Arrange
                ts.ActSessOptions.CacheRunnerAsTask=true;
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Act
                        runner = store.GetRunner<Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null);
                        //Assess
                        Assert.True(ReferenceEquals(runner_and_key.Runner, runner));
                    }
                }

                //Test case: search for an existing runner, cached as task, incompatible type
                //Arrange
                logger_mock = ts.InitLogger();
                logger_mock.MonitorLogEntry(LogLevel.Warning, UNASSIGNABLE_TYPE_WARNING_ID);
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Act
                        IActiveSessionRunner<String>? runner2 = store.GetRunner<String>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null);
                        //Assess
                        Assert.Null(runner2);
                        logger_mock.VerifyLogEntry(LogLevel.Warning, UNASSIGNABLE_TYPE_WARNING_ID, Times.Once());
                    }
                }
                logger_mock=ts.InitLogger();

            }
        }

        //Test group: GetRunnerAsync method call
        [Fact]
        public void GetRunnerAsync()
        {
            RunnerTestSetup ts;
            ActiveSessionStore store;
            KeyedActiveSessionRunner<Result1> runner_and_key;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts = null!;  //Inialize to avoid false error concerning use of an uninitialized variable
            IActiveSessionRunner<Result1>? runner;

            //Test case: search for an existing runner
            //Arrange
            using (ts=new RunnerTestSetup()) {
                ts.AddRunnerFactory<Request1, Result1>(
                    arg => {
                        dummy_runner1=new MockedRunner<Request1, Result1>(cts, arg);
                        return dummy_runner1.Runner;
                    }
                );
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Act
                        runner=store.GetRunnerAsync<Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null,
                            CancellationToken.None).GetAwaiter().GetResult();
                        //Assess
                        Assert.True(ReferenceEquals(runner_and_key.Runner, runner));

                        //Test case: search for a non-existing runner, no associated session values (already arranged)
                        //Act
                        runner=store.GetRunnerAsync<Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber+1,
                            null,
                            CancellationToken.None).GetAwaiter().GetResult();
                        //Assess
                        Assert.Null(runner);
                        //Test case: search for an already removed runner
                        //Arrange:Evict the runner from the cache (via Remove)
                        String runner_key = DEFAULT_SESSION_KEY_PREFIX+"_"+ts.MockSession.Object.Id+"_"+runner_and_key.RunnerNumber.ToString();
                        ts.Cache.CacheMock.Object.Remove(runner_key);
                        //Act
                        runner=store.GetRunnerAsync<Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null,
                            CancellationToken.None).GetAwaiter().GetResult();
                        //Assess
                        Assert.Null(runner);
                        //Check that ISesson variables have been cleared
                        Assert.Null(ts.MockSession.Object.GetString(runner_key));
                        Assert.Null(ts.MockSession.Object.GetString(runner_key+"_Type"));
                    }
                }

                //Test case: try to get a runner from the disposed store (Already arranged)
                //Act & assess
                Assert.Throws<ObjectDisposedException>(
                        () => {
                            store.GetRunnerAsync<Result1>(ts.MockSession.Object,
                                ts.StubActiveSession.Object,
                                ts.MockRunnerManager.Object,
                                runner_and_key.RunnerNumber,
                                null,
                                CancellationToken.None).GetAwaiter().GetResult();
                        }
                        );

                const int UNASSIGNABLE_TYPE_WARNING_ID = 1160;
                //Test case: search for an existing runner with incompatible type
                //Arrange
                MockedLogger logger_mock = ts.InitLogger();
                logger_mock.MonitorLogEntry(LogLevel.Warning, UNASSIGNABLE_TYPE_WARNING_ID);
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Act
                        IActiveSessionRunner<String>? runner2 = store.GetRunnerAsync<String>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null,
                            CancellationToken.None).GetAwaiter().GetResult();
                        //Assess
                        Assert.Null(runner2);
                        logger_mock.VerifyLogEntry(LogLevel.Warning, UNASSIGNABLE_TYPE_WARNING_ID, Times.Once());
                    }
                }
                logger_mock=ts.InitLogger();

                //Test case: search for an existing runner, cached as task
                //Arrange
                ts.ActSessOptions.CacheRunnerAsTask=true;
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Act
                        runner=store.GetRunnerAsync<Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null,
                            CancellationToken.None).GetAwaiter().GetResult();
                        //Assess
                        Assert.True(ReferenceEquals(runner_and_key.Runner, runner));
                    }
                }

                //Test case: search for an existing runner, cached as task, incompatible type
                //Arrange
                logger_mock=ts.InitLogger();
                logger_mock.MonitorLogEntry(LogLevel.Warning, UNASSIGNABLE_TYPE_WARNING_ID);
                using (store=ts.CreateStore()) {
                    using (cts=new CancellationTokenSource()) {
                        runner_and_key=store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            request,
                            null);
                        //Act
                        IActiveSessionRunner<String>? runner2 = store.GetRunnerAsync<String>(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null,
                            CancellationToken.None).GetAwaiter().GetResult();
                        //Assess
                        Assert.Null(runner2);
                        logger_mock.VerifyLogEntry(LogLevel.Warning, UNASSIGNABLE_TYPE_WARNING_ID, Times.Once());
                    }
                }
                logger_mock=ts.InitLogger();

            }
        }

        //TODO Test case: Create runner race conditions - ActiveSession level lock
        //TODO Test case: Create runner race conditions - store level lock

        /*More methods to test
        public Task<Boolean> TerminateSession(IActiveSession Session, Boolean Global);
        public IActiveSessionFeature CreateFeatureObject(ISession? Session, String? TraceIdentier);
        */


        /////////////////////////////////////////////////////////////////////////////////////////////
        //Auxilary clases
        /////////////////////////////////////////////////////////////////////////////////////////////
        class TestException : Exception
        {
            public Int32 Stage { get; init; }

            public TestException(Int32 Stage):base("Test exception for stage="+Stage.ToString())
            {
                this.Stage=Stage;
            }
        }

        class TestException1 : TestException {  public TestException1() : base(1) { } }
        class TestException2 : TestException { public TestException2() : base(2) { } }
        class TestException3 : TestException { public TestException3() : base(3) { } }
        class TestException4 : TestException { public TestException4() : base(4) { } }

        class MockedRunner<TRequest,TResult>
        {
            readonly Mock<IActiveSessionRunner<TResult>> _fakeRunner;
            readonly CancellationTokenSource _cts;

            public IActiveSessionRunner<TResult> Runner { get =>_fakeRunner.Object; }
            public readonly TRequest Arg;

            public MockedRunner(CancellationTokenSource Cts, TRequest Arg)
            {
                _cts=Cts;
                _fakeRunner=new Mock<IActiveSessionRunner<TResult>>();
                _fakeRunner.Setup(s => s.GetCompletionToken()).Returns(_cts.Token);
                this.Arg=Arg;
            }
        }

        class ServiceProviderMock
        {
            readonly Mock<IServiceScopeFactory> _fakeScopeFactory;
            readonly Mock<IServiceScope> _fakeServiceScope;
            readonly Expression<Action<IServiceScope>> _disposeExpression = s => s.Dispose();

            readonly Mock<IServiceProvider> _fakeSessionServiceProvider;

            public Boolean ScopeDisposed { get; private set; }
            public IServiceProvider ScopeServiceProvider
            {
                get
                {
                    if (ScopeDisposed)
                        throw new ObjectDisposedException("IServiceScope");
                    return _fakeSessionServiceProvider.Object;
                }
            }

            public ServiceProviderMock(Mock<IServiceProvider> RootServiceProviderMock)
            {
                _fakeSessionServiceProvider=new Mock<IServiceProvider>();
                _fakeSessionServiceProvider.Setup(s => s.GetService(It.IsAny<Type>()))
                    .Returns((Type x) => ScopeDisposed?throw new ObjectDisposedException("IServiceScope"):RootServiceProviderMock.Object.GetService(x));
                _fakeServiceScope=new Mock<IServiceScope>();
                _fakeServiceScope.SetupGet(s => s.ServiceProvider).Returns(ScopeServiceProvider);
                _fakeServiceScope.Setup(_disposeExpression).Callback(() => { ScopeDisposed=true; });

                _fakeScopeFactory=new Mock<IServiceScopeFactory>();
                _fakeScopeFactory.Setup(s => s.CreateScope())
                    .Callback(() => ScopeDisposed=false)
                    .Returns(_fakeServiceScope.Object);
                RootServiceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
                    .Returns(_fakeScopeFactory.Object);
            }
        }

        class MockedCache
        {
            public readonly Mock<IMemoryCache> CacheMock;
            public readonly Mock<ICacheEntry> EntryMock;
            public static readonly Expression<Func<IMemoryCache, ICacheEntry>> CreateEntryEnpression = s => s.CreateEntry(It.IsAny<Object>());
            public static readonly Expression<Func<IMemoryCache, Boolean>> TryGetValueExpression =
                s => s.TryGetValue(It.IsAny<Object>(), out It.Ref<Object>.IsAny);

            public Action? DisposeTrap { get; set; }
            public Action? CreateEntryTrap { get; set; }

            Boolean _isEntryStored;
            Object? _key;
            Object? _value;
            DateTimeOffset? _absoluteExpiration;
            TimeSpan? _absoluteExpirationRelativeToNow;
            readonly IList<IChangeToken> _expirationTokens = new List<IChangeToken>();
            readonly IList<PostEvictionCallbackRegistration> _postEvictionCallbacks = new List<PostEvictionCallbackRegistration>();
            CacheItemPriority _priority=CacheItemPriority.Normal;
            Int64? _size;
            TimeSpan? _slidingExpiration;
            IDisposable? _subscription = null;
            Int32 _calledCallbacksCount=0;

            public MockedCache()
            {
                CacheMock=new Mock<IMemoryCache>();
                EntryMock=new Mock<ICacheEntry>();

                CacheMock.Setup(CreateEntryEnpression)
                    .Callback((Object key) => { CreateEntryTrap?.Invoke(); ClearStoredEntry(); _key=key; })
                    .Returns(EntryMock.Object);
                CacheMock.Setup(TryGetValueExpression)
                    .Callback((Object _, ref Object value) => { value=_value!; })
                    .Returns((Object key, ref Object _ ) => _isEntryStored&&key.Equals(_key));
                CacheMock.Setup(s => s.Remove(It.IsAny<Object>()))
                    .Callback((Object Key) => { CheckKey(Key); Evict(EvictionReason.Removed); });

                EntryMock.Setup(s => s.Dispose())
                    .Callback(() => { if (!_isEntryStored&&EntryMock.Object.Value!=null) {
                            DisposeTrap?.Invoke();
                            StoreEntry(); 
                        }});
                EntryMock.SetupProperty(s => s.Value);
                EntryMock.SetupProperty(s => s.AbsoluteExpiration);
                EntryMock.SetupProperty(s => s.AbsoluteExpirationRelativeToNow);
                EntryMock.SetupGet(s => s.ExpirationTokens).Returns(_expirationTokens);
                EntryMock.SetupGet(s=>s.PostEvictionCallbacks).Returns(_postEvictionCallbacks);
                EntryMock.SetupProperty(s=>s.Priority).Object.Priority = CacheItemPriority.Normal;
                EntryMock.SetupProperty(s => s.Size);
                EntryMock.SetupProperty(s => s.SlidingExpiration);
            }

            void Evict(Object State)
            {
                EvictionReason reason = (EvictionReason)State;
                if (_isEntryStored) {
                    List<PostEvictionCallbackRegistration> callbacks = _postEvictionCallbacks.ToList();
                    Object? old_value = _value;
                    ClearStoredEntry();
                    foreach (PostEvictionCallbackRegistration reg in callbacks) {
                        reg.EvictionCallback(_key, old_value, reason, reg.State);
                        _calledCallbacksCount++;
                    }
                }
            }

            void CheckKey(Object Key)
            {
                if (Key==null) throw new ArgumentNullException(nameof(Key));
                if (_isEntryStored&&!Key.Equals(_key))
                    throw new InvalidOperationException("Mock limitation: cannot operate with another entry while one is stored already.");
            }

            void ClearStoredEntry()
            {
                _isEntryStored=false;
                _value=null;
                _absoluteExpiration=null;
                _absoluteExpirationRelativeToNow=null;
                _expirationTokens.Clear();
                _postEvictionCallbacks.Clear();
                _priority = CacheItemPriority.Normal;
                _size=null;
                _slidingExpiration=null;
                _subscription?.Dispose();
                _subscription=null;
            }

            void StoreEntry()
            {
                _value=EntryMock.Object.Value;
                _absoluteExpiration=EntryMock.Object.AbsoluteExpiration;
                _absoluteExpirationRelativeToNow=EntryMock.Object.AbsoluteExpirationRelativeToNow;
                _priority=EntryMock.Object.Priority;
                _size=EntryMock.Object.Size;
                _slidingExpiration=EntryMock.Object.SlidingExpiration;
                CompositeChangeToken token = new CompositeChangeToken(_expirationTokens as IReadOnlyList<IChangeToken>);
                if (token.ActiveChangeCallbacks)
                    _subscription=token.RegisterChangeCallback(Evict, EvictionReason.TokenExpired);
                _calledCallbacksCount=0;
                _isEntryStored=true;
            }

            public  Boolean IsEntryStored { get => _isEntryStored; }
            public Int32 CalledCallbacksCount { get => _calledCallbacksCount; }
            //Tracked ICacheEntry properties
            public Object? Value { get => _value; }
            public DateTimeOffset? AbsoluteExpiration { get => _absoluteExpiration; }
            public TimeSpan? AbsoluteExpirationRelativeToNow { get => _absoluteExpirationRelativeToNow; }
            public IList<IChangeToken> ExpirationTokens { get =>_expirationTokens; }
            public Object Key { get => _key!; }
            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get => _postEvictionCallbacks; } 
            public CacheItemPriority Priority { get => _priority; }
            public Int64? Size { get => _size; }
            public TimeSpan? SlidingExpiration { get => _slidingExpiration; }
        }

        class ConstructorTestSetup
        {
            public ActiveSessionOptions ActSessOptions = new ActiveSessionOptions();
            public SessionOptions SessOptions = new SessionOptions();
            public readonly IOptions<ActiveSessionOptions> IActSessionOptions;
            public readonly IOptions<SessionOptions> ISessOptions;
            public readonly Mock<IMemoryCache>? MockCache;
            public readonly Mock<IRunnerManagerFactory> StubRMFactory;
            public readonly Mock<IRunnerManager> MockRunnerManager;

            protected MockedLoggerFactory _loggerFactory;
            protected MockedLogger _logger;
            public IServiceProvider RootSP { get { return MockRootServiceProvider.Object; } }
            public Object LockObject { get => _lockObject; }

            public readonly Mock<IServiceProvider> MockRootServiceProvider;
            readonly Object _lockObject=new Object();

            public Boolean LoggerCreated() {
                return _loggerFactory.LoggerCreationCount(LOGGING_CATEGORY_NAME)>0;
            }

            public ConstructorTestSetup(Mock<IMemoryCache>? MockCache)
            {
                MockRootServiceProvider=new Mock<IServiceProvider>();
                _loggerFactory=new MockedLoggerFactory();
                _logger=_loggerFactory.MonitorLoggerCategory(LOGGING_CATEGORY_NAME);
                this.MockCache=MockCache;
                IActSessionOptions=Options.Create(ActSessOptions);
                ISessOptions=Options.Create(SessOptions);
                MockRunnerManager=new Mock<IRunnerManager>();
                MockRunnerManager.SetupGet(s => s.RunnerCreationLock).Returns(_lockObject);
                MockRunnerManager.Setup(s => s.PerformRunnersCleanupAsync(It.IsAny<IActiveSession>())).Returns(Task.CompletedTask);
                StubRMFactory=new Mock<IRunnerManagerFactory>();
                StubRMFactory.Setup(s => s.GetRunnerManager(It.IsAny<ILogger>(),
                    It.IsAny<IServiceProvider>(), It.IsAny<Int32>(), It.IsAny<Int32>())).Returns(MockRunnerManager.Object);
            }

            public ActiveSessionStore CreateStore()
            {
                IMemoryCache? cache = MockCache?.Object;
                return new ActiveSessionStore(
                    cache,
                    MockRootServiceProvider.Object,
                    StubRMFactory.Object, 
                    IActSessionOptions,
                    ISessOptions,
                    _loggerFactory.LoggerFactory);
            }
        }

        class MockedCaheTestSetup : ConstructorTestSetup
        {
            public readonly MockedCache Cache;
            public const String TEST_SESSION_ID = "TestSessionId";


            protected MockedCaheTestSetup(MockedCache Cache) : base(Cache.CacheMock)
            {
                this.Cache=Cache;
            }


        }

        class CreateFetchTestSetup : MockedCaheTestSetup, IDisposable
        {
            //TODO Monitor AbortAll call
            public readonly Mock<ISession> StubSession;
            public Boolean ScopeDisposed { get { return _mockedSessionServiceProvider.ScopeDisposed; } }
            public IServiceProvider ScopeServiceProvider { get { return _mockedSessionServiceProvider.ScopeServiceProvider; } }
            public readonly Expression<Action<IRunnerManager>> RegisterSessionExpression = (s => s.RegisterSession(It.IsAny<IActiveSession>()));
            public readonly Expression<Action<IRunnerManager>> AbortAllExpression = s => s.AbortAll(It.IsAny<IActiveSession>());
            public readonly Expression<Func<IRunnerManager, Task>> PerformRunnersCleanupExpression =
                s => s.PerformRunnersCleanupAsync(It.IsAny<IActiveSession>());

            readonly CancellationTokenSource _cts;
            readonly ServiceProviderMock _mockedSessionServiceProvider;


            public CreateFetchTestSetup() : base(new MockedCache())
            {
                StubSession=new Mock<ISession>();
                StubSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                _mockedSessionServiceProvider=new ServiceProviderMock(MockRootServiceProvider);
                _cts=new CancellationTokenSource();
                MockRunnerManager.Setup(RegisterSessionExpression);
                MockRunnerManager.Setup(AbortAllExpression);
                MockRunnerManager.Setup(PerformRunnersCleanupExpression).Returns(Task.CompletedTask);
            }

            public void Dispose()
            {
                _cts.Dispose();
            }
        }

        class RunnerTestSetup : MockedCaheTestSetup, IDisposable
        {
            //TODO Monitor Abort call
            public readonly Mock<ISession> MockSession;
            public readonly Mock<IActiveSession> StubActiveSession;
            readonly Object? _lockObject = null;
            readonly CancellationTokenSource _cts;

            public readonly Expression<Func<IRunnerManager, Int32>> GetRunnerNumberExpression;
            public readonly Expression<Action<IRunnerManager>> ReturnRunnerNumberExpression;
            public readonly Expression<Action<IRunnerManager>> RegisterRunnerExpression;
            public readonly Expression<Action<IRunnerManager>> UnregisterRunnerExpression;
            public readonly Expression<Func<IRunnerManager,Object?>> RunnerCreationLockExpression = (s => s.RunnerCreationLock);
            public readonly Expression<Action<ISession>> SessionKeyRemoveExpression= s => s.Remove(It.IsAny<String>());

            public const Int32 RUNNER_1 = 1;
            public Dictionary<String, byte[]> _session_values = new Dictionary<String, byte[]>();

            public Action? CreateStage1Callback { get; set; }
            public Action? CreateStage3Callback { get; set; }
            public Action? CreateStage4Callback { get; set; }

            public MockedLogger InitLogger()
            {
                _loggerFactory.ResetAllCategories();
                _logger=_loggerFactory.MonitorLoggerCategory(LOGGING_CATEGORY_NAME);
                return _logger;
            }

            public RunnerTestSetup(Boolean PerSessionLock=true) : base(new MockedCache()) 
            {
                MockSession=new Mock<ISession>();
                MockSession.SetupGet(s => s.IsAvailable).Returns(true);
                MockSession.SetupGet(s=>s.Id).Returns(TEST_SESSION_ID);
                MockSession.Setup(SessionKeyRemoveExpression)
                    .Callback((String key) => { _session_values.Remove(key); });
                MockSession.Setup(s => s.Set(It.IsAny<String>(), It.IsAny<byte[]>()))
                    .Callback((String key, byte[] value) => {
                        CreateStage3Callback?.Invoke();
                        if (_session_values.ContainsKey(key)) _session_values[key] = value; 
                        else _session_values.Add(key, value); 
                    });
                MockSession.Setup(s => s.TryGetValue(It.IsAny<String>(), out It.Ref<Byte[]?>.IsAny))
                    .Returns((String key,out byte[]? value)=> { 
                            if(_session_values.ContainsKey(key)) {
                                value=_session_values[key];
                                return true;
                            }
                            else {
                                value=null;
                                return false;
                            }
                        });

                ;
                //TODO Setup methods for ISession calls verification 
                StubActiveSession=new Mock<IActiveSession>();
                _cts=new CancellationTokenSource();
                StubActiveSession.SetupGet(s => s.CompletionToken).Returns(_cts.Token);
                StubActiveSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                if (PerSessionLock) _lockObject=new Object();
                GetRunnerNumberExpression=(s => s.GetNewRunnerNumber(StubActiveSession.Object, It.IsAny<String>()));
                ReturnRunnerNumberExpression=(s => s.ReturnRunnerNumber(StubActiveSession.Object, It.IsAny<Int32>()));
                RegisterRunnerExpression=(s => s.RegisterRunner(StubActiveSession.Object, It.IsAny<Int32>(), It.IsAny<IActiveSessionRunner>(),It.IsAny<Type>()));
                UnregisterRunnerExpression=(s => s.UnregisterRunner(StubActiveSession.Object, It.IsAny<Int32>()));
                MockRunnerManager.Setup(GetRunnerNumberExpression)
                    .Callback((IActiveSession _, String _) => { CreateStage1Callback?.Invoke(); })
                    .Returns(RUNNER_1);
                MockRunnerManager.Setup(ReturnRunnerNumberExpression);
                MockRunnerManager.Setup(RegisterRunnerExpression)
                    .Callback((IActiveSession _,Int32 _,IActiveSessionRunner _, Type _) => { CreateStage4Callback?.Invoke(); });
                MockRunnerManager.Setup(UnregisterRunnerExpression);
                MockRunnerManager.SetupGet(RunnerCreationLockExpression).Returns(_lockObject);
            }

            public Expression<Func<IServiceProvider, Object?>> RunnerFactoryExpression<TRequest, TResult>()
            {
                return s => s.GetService(typeof(IActiveSessionRunnerFactory<TRequest, TResult>));
            }

            IActiveSessionRunnerFactory<TRequest,TResult> MockRunnerFactory<TRequest, TResult>(
                Func<TRequest, IActiveSessionRunner<TResult>> Factory)
            {
                Mock<IActiveSessionRunnerFactory<TRequest, TResult>>? factory_mock = new ();
                factory_mock.Setup(s => s.Create(It.IsAny<TRequest>(), It.IsAny<IServiceProvider>()))
                    .Returns((TRequest r,IServiceProvider sp) => Factory(r));
                return factory_mock.Object;
            }

            public void AddRunnerFactory<TRequest,TResult>(Func<TRequest,IActiveSessionRunner<TResult>> Factory)
            {
                MockRootServiceProvider.Setup(RunnerFactoryExpression<TRequest, TResult>())
                    .Returns(MockRunnerFactory(Factory));
            }

            public void Dispose()
            {
                _cts.Dispose();
            }

        }

    }
}
