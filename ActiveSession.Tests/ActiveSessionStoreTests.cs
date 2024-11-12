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
using static MVVrus.AspNetCore.ActiveSession.Internal.LogIds;
using LogValues = System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>;
using Microsoft.Extensions.Internal;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

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
            Mock<IActiveSessionIdSupplier> dummy_id= new Mock<IActiveSessionIdSupplier>();
            ts=new ConstructorTestSetup(dummy_cache);

            //Test case: null IMemoryCahe argument while own caches is not used
            Assert.Throws<InvalidOperationException>(() => new ActiveSessionStore(
                null, ts.RootSP, ts.StubRMFactory.Object, ts.IActSessionOptions, ts.ISessOptions, ts.HostAppLifetime, dummy_id.Object));

            //Test case: null IServiceProvider argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, null!, ts.StubRMFactory.Object, ts.IActSessionOptions, ts.ISessOptions, ts.HostAppLifetime, dummy_id.Object));

            //Test case: null IRunnerManagerFactory argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, ts.RootSP, null!, ts.IActSessionOptions, ts.ISessOptions, ts.HostAppLifetime, dummy_id.Object));

            //Test case: null IOptions<ActiveSessionOptions> argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, ts.RootSP, ts.StubRMFactory.Object, null!, ts.ISessOptions, ts.HostAppLifetime, dummy_id.Object));

            //Test case: null IOptions<SessionOptions> argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, ts.RootSP, ts.StubRMFactory.Object, ts.IActSessionOptions, null!, ts.HostAppLifetime, dummy_id.Object));

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

            //Arrange
            using (ts=new CreateFetchTestSetup()) {
                using (store=ts.CreateStore()) {
                    //Test case: create new ActiveSession
                    //Act
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    //Assess
                    Assert.NotNull(session);
                    //Assess IActiveSession
                    Assert.Equal(CreateFetchTestSetup.TEST_ACTIVESESSION_ID, session.Id);
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
                    Assert.Equal(DEFAULT_SESSION_KEY_PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID, ts.Cache.Key);
                    Assert.True(ReferenceEquals(session, ts.Cache.Value));
                    Assert.Equal(s_defaultIdleTimeout, ts.Cache.SlidingExpiration);
                    Assert.Equal(DEFAULT_MAX_LIFETIME, ts.Cache.AbsoluteExpirationRelativeToNow);
                    Assert.Null(ts.Cache.AbsoluteExpiration);
                    Assert.Equal(CacheItemPriority.Normal, ts.Cache.Priority);
                    Assert.Equal(1, ts.Cache.ExpirationTokens.Count);
                    Assert.True(ts.Cache.ExpirationTokens[0].ActiveChangeCallbacks);
                    Assert.False(ts.Cache.ExpirationTokens[0].HasChanged);
                    Assert.Equal(1, ts.Cache.PostEvictionCallbacks.Count);
                    Assert.Equal(CreateFetchTestSetup.TEST_ACTIVESESSION_ID, session.BaseId);

                    //Test case: fetch ActiveSession from cache
                    IActiveSession? session2 = store.FetchOrCreateSession(ts.MockSession.Object, null, null);
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
                    Assert.Throws<ObjectDisposedException>(() => session_sp.GetService(typeof(ILoggerFactory)));
                }

                //Test case: options passed to ActiveSessionStore constructor affects cache entry
                TimeSpan EXPIRATION = TimeSpan.FromMinutes(1);
                TimeSpan RUNNER_EXPIRATION = TimeSpan.FromSeconds(30);
                TimeSpan MAX_LIFETIME = TimeSpan.FromHours(1);
                String PREFIX = "TestPrefix";
                Int32 AS_SIZE = 2;
                //Arrange
                ts.SessOptions.IdleTimeout=EXPIRATION;
                ts.ActSessOptions.RunnerIdleTimeout=RUNNER_EXPIRATION;
                ts.ActSessOptions.MaxLifetime=MAX_LIFETIME;
                ts.ActSessOptions.Prefix=PREFIX;
                ts.ActSessOptions.TrackStatistics=true;
                ts.ActSessOptions.ActiveSessionSize=AS_SIZE;
                using(store=ts.CreateStore()) {
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    //Assess
                    Assert.NotNull(session);
                    Assert.True(ts.Cache.IsEntryStored);
                    Assert.Equal(PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID, ts.Cache.Key);
                    Assert.Equal(EXPIRATION, ts.Cache.SlidingExpiration);
                    Assert.Equal(MAX_LIFETIME, ts.Cache.AbsoluteExpirationRelativeToNow);
                    Assert.Equal(AS_SIZE, store.GetCurrentStatistics()!.StoreSize);

                    //Test case: removing ActiveSession from cache. 
                    //Arrange more
                    IServiceProvider session_sp = session.SessionServices;
                    //Act
                    ts.Cache.CacheMock.Object.Remove(PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID);
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
                }

                const Int32 CLEANUP_TIMEOUT = 2000;
                //Test case: logging timely active session cleanup after eviction - ended in time
                //Arrange
                ts.ActSessOptions.CleanupLoggingTimeoutMs=CLEANUP_TIMEOUT;
                Boolean? in_time;
                Action<LogLevel, EventId, LogValues> log_callback = (_, _, vals) => in_time=(Boolean?)(vals[1].Value);
                MockedLogger mock_logger;
                mock_logger=ts.InitLogger();
                in_time=null;
                mock_logger.MonitorLogEntry(LogLevel.Debug, D_STORERUNNERCLEANUPRESULT, log_callback);
                ts.SetCleanupCallback(() => { Thread.Sleep(CLEANUP_TIMEOUT/2); });
                using (store=ts.CreateStore()) {
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    //Act
                    ts.Cache.CacheMock.Object.Remove(PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID);
                    session!.CleanupCompletionTask.GetAwaiter().GetResult();
                    store._cleanupLoggingTask?.GetAwaiter().GetResult();
                    //Assess
                    Assert.Equal(true, in_time);
                }

                //Test case: logging timely active session cleanup after eviction - timeout
                mock_logger=ts.InitLogger();
                in_time=null;
                mock_logger.MonitorLogEntry(LogLevel.Debug, D_STORERUNNERCLEANUPRESULT, log_callback);
                ts.SetCleanupCallback(() => { Thread.Sleep(CLEANUP_TIMEOUT*3/2); });
                using (store=ts.CreateStore()) {
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    //Act
                    ts.Cache.CacheMock.Object.Remove(PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID);
                    session!.CleanupCompletionTask.GetAwaiter().GetResult();
                    store._cleanupLoggingTask?.GetAwaiter().GetResult();
                    //Assess
                    Assert.Equal(false, in_time);
                }

                //Test case: logging active session runners cleanup - no timeout set
                //Arrange
                ts.ActSessOptions.CleanupLoggingTimeoutMs=null;
                mock_logger=ts.InitLogger();
                in_time=null;
                mock_logger.MonitorLogEntry(LogLevel.Debug, D_STORERUNNERCLEANUPRESULT, log_callback);
                ts.SetCleanupCallback(() => { Thread.Sleep(CLEANUP_TIMEOUT*3/2); });
                using(store=ts.CreateStore()) {
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    //Act
                    ts.Cache.CacheMock.Object.Remove(PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID);
                    session!.CleanupCompletionTask.GetAwaiter().GetResult();
                    store._cleanupLoggingTask?.GetAwaiter().GetResult();
                    //Assess
                    Assert.Equal(true, in_time);
                }
            }
        }

        //Test group: creating or fetching from cache an active session with suffix
        [Fact]
        public void CreateOrFetchSessionWithSuffix()
        {
            CreateFetchTestSetup ts;
            ActiveSessionStore store;
            IActiveSession? session;

            const String SUFFIX = "Test";

            //Arrange
            using(ts=new CreateFetchTestSetup()) {
                using(store=ts.CreateStore()) {

                    //Test case: create a new ActiveSession
                    //Act
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, SUFFIX);
                    //Assess
                    Assert.NotNull(session);
                    //Assess IActiveSession
                    String id_to_be= CreateFetchTestSetup.TEST_ACTIVESESSION_ID+"-"+SUFFIX;
                    Assert.Equal(id_to_be, session.Id);
                    Assert.Equal(CreateFetchTestSetup.TEST_ACTIVESESSION_ID, session.BaseId);
                    //Assess a cache entry
                    Assert.True(ts.Cache.IsEntryStored);
                    Assert.Equal(DEFAULT_SESSION_KEY_PREFIX+"_"+id_to_be, ts.Cache.Key);
                    Assert.True(ReferenceEquals(session, ts.Cache.Value));

                    //Test case: fetch ActiveSession from cache
                    //Act
                    IActiveSession? session2 = store.FetchOrCreateSession(ts.MockSession.Object, null, SUFFIX);
                    //Assess
                    Assert.Same(session, session2);
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
                        task1=Task.Run(() => { event1.Set(); return store.FetchOrCreateSession(ts.MockSession.Object, null, null)!; });
                        if (!event1.WaitOne(2000))
                            throw new Exception("Deadlock detected");
                        task2=Task.Run(() => { event2.Set(); return store.FetchOrCreateSession(ts.MockSession.Object, null, null)!; });
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
                        Assert.Equal(DEFAULT_SESSION_KEY_PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID, ts.Cache.Key);
                    }

                }
                finally {
                    proceed_event?.Dispose();
                    event1.Dispose();
                    event2.Dispose();
                }
            }
        }

        //Test group: test normal flow around CreateRunner method called for an ActiveSessinonStore object created with default options
        [Fact]
        public void CreateRunner()
        {
            RunnerTestSetup ts;
            ActiveSessionStore store;
            KeyedRunner<Result1> runner_and_key;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts = null!;  //Inialize to avoid false error concerning use of an uninitialized variable

            //Test case: create new runner with ActiveSession level lock and default options
            //Arrange
            using (ts=new RunnerTestSetup()) {
                ts.AddRunnerFactory<Request1, Result1>(
                    arg =>
                    {
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
                        String runner_key = RunnerKey();
                        Assert.Equal(DEFAULT_HOST_NAME, ts.MockSession.Object.GetString(runner_key));
                        Assert.Equal(typeof(Result1).FullName, ts.MockSession.Object.GetString(runner_key+"_Type"));
                        //Check cache entry
                        ts.Cache.CacheMock.Verify(MockedCache.CreateEntryEnpression, Times.Once);
                        Assert.True(ts.Cache.IsEntryStored);
                        Assert.Equal(runner_key, ts.Cache.Key);
                        Assert.True(ReferenceEquals(runner_and_key.Runner, ts.Cache.Value));
                        Assert.Equal(DEFAULT_RUNNER_IDLE_TIMEOUT, ts.Cache.SlidingExpiration);
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
                        //Check the runner was aborted
                        dummy_runner1!.VerifyAbort();
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
                    () =>
                    {
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
                        arg =>
                        {
                            stage2_callback?.Invoke();
                            return (dummy_runner1=new MockedRunner<Request1, Result1>(cts, arg)).Runner;
                        }
                    );
                    using (store=ts.CreateStore()) {
                        //Act & assess stage 1
                        Assert.Throws<TestException1>(
                            () => store.CreateRunner<Request1, Result1>(ts.MockSession.Object,
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

        //Test group: call CreateRunner method for an ActiveSessinonStore object created with non-default options
        [Fact]
        public void CreateRunner_NonDefaultOptions()
        {
            RunnerTestSetup ts;
            ActiveSessionStore store;
            KeyedRunner<Result1> runner_and_key;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts = null!;  //Inialize to avoid false error concerning use of an uninitialized variable

            //Test case: create new runner with store level lock and custom options
            TimeSpan EXPIRATION = TimeSpan.FromMinutes(1);
            TimeSpan RUNNER_EXPIRATION = TimeSpan.FromSeconds(30);
            TimeSpan MAX_LIFETIME = TimeSpan.FromHours(1);
            Int32 ASR_SIZE = 10;
            //Arrange
            using (ts=new RunnerTestSetup()) {
                ts.SessOptions.IdleTimeout=EXPIRATION;
                ts.ActSessOptions.RunnerIdleTimeout=RUNNER_EXPIRATION;
                ts.ActSessOptions.MaxLifetime=MAX_LIFETIME;
                ts.ActSessOptions.Prefix=DEFAULT_SESSION_KEY_PREFIX;
                ts.ActSessOptions.TrackStatistics=true;
                ts.ActSessOptions.DefaultRunnerSize=ASR_SIZE;
                ts.AddRunnerFactory<Request1, Result1>(
                    arg =>
                    {
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
                        //Check cache entry
                        ts.Cache.CacheMock.Verify(MockedCache.CreateEntryEnpression, Times.Once);
                        Assert.True(ts.Cache.IsEntryStored);
                        String runner_key = RunnerKey();
                        Assert.Equal(runner_key, ts.Cache.Key);
                        Assert.Equal(RUNNER_EXPIRATION, ts.Cache.SlidingExpiration);
                        Assert.Equal(MAX_LIFETIME, ts.Cache.AbsoluteExpirationRelativeToNow);
                        Assert.Equal(ASR_SIZE, store.GetCurrentStatistics()!.StoreSize);

                        //Test case: Evict the runner from the cache (via Remove) and check StoreSize (already arranged)
                        //Act
                        ts.Cache.CacheMock.Object.Remove(runner_key);
                        //Assess
                        Assert.Equal(0, store.GetCurrentStatistics()!.StoreSize);
                        Assert.False(ts.Cache.IsEntryStored);
                        Assert.Equal(1, ts.Cache.CalledCallbacksCount);
                        //Check the runner was aborted
                        dummy_runner1!.VerifyAbort();
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
            KeyedRunner<Result1> runner_and_key;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts = null!;  //Inialize to avoid false error concerning use of an uninitialized variable
            IRunner? runner;

            //Test case: search for an existing runner
            //Arrange
            using (ts=new RunnerTestSetup()) {
                ts.AddRunnerFactory<Request1, Result1>(
                    arg =>
                    {
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
                        runner=store.GetRunner(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null);
                        //Assess
                        Assert.True(ReferenceEquals(runner_and_key.Runner, runner));

                        //Test case: search for a non-existing runner, no associated session values (already arranged)
                        //Act
                        runner=store.GetRunner(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber+1,
                            null);
                        //Assess
                        Assert.Null(runner);
                        //Test case: search for an already removed runner
                        //Arrange:Evict the runner from the cache (via Remove)
                        String runner_key = RunnerKey();
                        ts.Cache.CacheMock.Object.Remove(runner_key);
                        //Act
                        runner=store.GetRunner(ts.MockSession.Object,
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
                    () => {
                        store.GetRunner(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null
                        );
                    }
                );
            }
        }

        //Test group: GetRunnerAsync method call
        [Fact]
        public void GetRunnerAsync()
        {
            RunnerTestSetup ts;
            ActiveSessionStore store;
            KeyedRunner<Result1> runner_and_key;
            Request1 request = new Request1() { Arg=TEST_ARG1 };
            MockedRunner<Request1, Result1>? dummy_runner1 = null;
            CancellationTokenSource cts = null!;  //Inialize to avoid false error concerning use of an uninitialized variable
            IRunner? runner;

            //Test case: search for an existing runner
            //Arrange
            using (ts=new RunnerTestSetup()) {
                ts.AddRunnerFactory<Request1, Result1>(
                    arg =>
                    {
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
                        runner=store.GetRunnerAsync(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber,
                            null,
                            CancellationToken.None).GetAwaiter().GetResult();
                        //Assess
                        Assert.True(ReferenceEquals(runner_and_key.Runner, runner));

                        //Test case: search for a non-existing runner, no associated session values (already arranged)
                        //Act
                        runner=store.GetRunnerAsync(ts.MockSession.Object,
                            ts.StubActiveSession.Object,
                            ts.MockRunnerManager.Object,
                            runner_and_key.RunnerNumber+1,
                            null,
                            CancellationToken.None).GetAwaiter().GetResult();
                        //Assess
                        Assert.Null(runner);
                        //Test case: search for an already removed runner
                        //Arrange:Evict the runner from the cache (via Remove)
                        String runner_key = RunnerKey();
                        ts.Cache.CacheMock.Object.Remove(runner_key);
                        //Act
                        runner=store.GetRunnerAsync(ts.MockSession.Object,
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
                        () =>
                        {
                            store.GetRunnerAsync(ts.MockSession.Object,
                                ts.StubActiveSession.Object,
                                ts.MockRunnerManager.Object,
                                runner_and_key.RunnerNumber,
                                null,
                                CancellationToken.None).GetAwaiter().GetResult();
                        }
                    );

            }
        }

        //Test case: call Terminate method
        [Fact]
        public void TerminateSession()
        {
            //Arrange
            using (CreateFetchTestSetup ts = new CreateFetchTestSetup()) {
                using (ActiveSessionStore store = ts.CreateStore()) {
                    IActiveSession session = store.FetchOrCreateSession(ts.MockSession.Object, null, null)!;
                    Task cleanup_task = session.CleanupCompletionTask;
                    //Act
                    Task terminate_task=store.TerminateSession(ts.MockSession.Object,session, ts.MockRunnerManager.Object, null);
                    //Assess
                    Assert.True(ReferenceEquals(cleanup_task, terminate_task));
                    terminate_task.GetAwaiter().GetResult();
                    Assert.Equal(-1, ts.MockSession.Object.GetInt32(DEFAULT_SESSION_KEY_PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID));
                    ts.MockRunnerManager.Verify(ts.AbortAllExpression, Times.AtLeastOnce);
                    Assert.False(ts.Cache.IsEntryStored);
                    Assert.Equal(1, ts.Cache.CalledCallbacksCount);
                    Assert.True((session as Active_Session)!.Disposed);
                }
            }
        }

        //Test group: create or fetch ActiveSession while it was marked terminated earlier
        [Fact]
        public void CreateFetchSession_Terminated()
        {
            CreateFetchTestSetup ts;
            ActiveSessionStore store;
            IActiveSession? session;

            //Test case: fetch ActiveSession from cache after the termination var in ISession has been set
            //Arrange
            using (ts=new CreateFetchTestSetup()) {
                using (store=ts.CreateStore()) {
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    Assert.NotNull(session);
                    ts.MockRunnerManager.Verify(ts.AbortAllExpression, Times.Never);
                    IActiveSession old_session = session;
                    Task cleanup_task = session.CleanupCompletionTask;
                    ts.MockSession.Object.SetInt32(DEFAULT_SESSION_KEY_PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID, -1);
                    //Act
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    //Assess
                    Assert.NotNull(session);
                    Assert.Equal(2, session.Generation);
                    Assert.NotEqual(TaskStatus.Created, cleanup_task.Status);
                    cleanup_task.GetAwaiter().GetResult();
                    Assert.True(ts.Cache.IsEntryStored);
                    Assert.True((old_session as Active_Session)!.Disposed);
                    ts.MockRunnerManager.Verify(ts.AbortAllExpression, Times.AtLeastOnce);
                    ts.MockRunnerManager.Verify(ts.PerformRunnersCleanupExpression, Times.Once);
                    Assert.Equal(1, ts.ScopeDisposeCount);
                }

                //Create ActiveSession after the termination var in ISession is set
                //Arrange
                using (store=ts.CreateStore()) {
                    ts.MockSession.Object.SetInt32(DEFAULT_SESSION_KEY_PREFIX+"_"+CreateFetchTestSetup.TEST_ACTIVESESSION_ID, -1);
                    //Act
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    //Assess
                    Assert.NotNull(session);
                    Assert.Equal(2, session.Generation);
                }
            }
        }

        //Test group: call CreateFeatureObject method
        [Fact]
        public void CreateFeatureObject()
        {
            const string TEST_SUFFIX="TestSuffix";
            //Arrange
            Mock<ISession> dummy_session = new Mock<ISession>();
            MockedCache cache_mock = new MockedCache();
            ConstructorTestSetup ts = new ConstructorTestSetup(cache_mock.CacheMock);
            ActiveSessionFeature feature_impl;
            //Test case: no suffix
            using (ActiveSessionStore store=ts.CreateStore()) {
                //Act
                IActiveSessionFeature feature = store.AcquireFeatureObject(dummy_session.Object,null,null);
                //Assess
                feature_impl=Assert.IsType<ActiveSessionFeature>(feature);
                Assert.Null(feature_impl.Suffix);
            }
            //Test case: specify suffix
            using(ActiveSessionStore store = ts.CreateStore()) {
                //Act
                IActiveSessionFeature feature = store.AcquireFeatureObject(dummy_session.Object, null, TEST_SUFFIX);
                //Assess
                feature_impl=Assert.IsType<ActiveSessionFeature>(feature);
                Assert.Equal(TEST_SUFFIX,feature_impl.Suffix);
            }
        }

        //Test group: test expirations from real cache
        [Fact]
        public void OwnCacheExpirations()
        {
            Task session_cleanup_task;
            IActiveSession session;
            const string ID1 = "Id1";
            ActiveSessionStoreStats stat;
            //Arrange common stuff
            OwnCacheTestSetup ts = new OwnCacheTestSetup();
            //Test case: lonely ActiveSession expired
            //Arrange
            using (ActiveSessionStore store = ts.CreateStore()) {
                session=store.FetchOrCreateSession(ts.MockSession.Object, null, null)??throw new Exception("Cannot create ActiveSession");
                session_cleanup_task=session.CleanupCompletionTask;
                Assert.Equal(1, store.GetCurrentStatistics()!.SessionCount);
                //Act 
                ts.Clock.Advance(OwnCacheTestSetup.SESSION_IDLE);
                store.InitCacheExpiration();
                //Assess
                Assert.Equal(0, Task.WaitAny(session_cleanup_task, Task.Delay(10000)));
                Assert.Equal(0, store.GetCurrentStatistics()!.SessionCount);
                Assert.True((session as Active_Session)!.Disposed);
                ts.Clock.Reset();
            }

            //Test case: a runner expired before its active session
            //Arrange
            using (ActiveSessionStore store = ts.CreateStore()) {
                session = store.FetchOrCreateSession(ts.MockSession.Object, null, null)??throw new Exception("Cannot create ActiveSession");
                session_cleanup_task=session.CleanupCompletionTask;
                Assert.Equal(1, store.GetCurrentStatistics()!.SessionCount);
                KeyedRunner<Result1> keyed_runner = store.CreateRunner<String, Result1>(
                    ts.MockSession.Object,
                    session,
                    (session as Active_Session)!.RunnerManager,
                    ID1,
                    null);
                Assert.Equal(1, store.GetCurrentStatistics()!.RunnerCount);
                SpyRunnerX runner = (SpyRunnerX)keyed_runner.Runner;
                Task disp_task = runner.DisposeTask;
                //Act
                ts.Clock.Advance(OwnCacheTestSetup.STEP);
                store.InitCacheExpiration();
                //Assess
                Assert.Equal(0, Task.WaitAny(disp_task, Task.Delay(10000)));
                stat = store.GetCurrentStatistics()!;
                Assert.Equal(1, stat.SessionCount);
                Assert.Equal(0, stat.RunnerCount);
                Assert.True(runner.Disposed);
                Assert.Null(store.GetRunner(
                    ts.MockSession.Object,
                    session,
                    (session as Active_Session)!.RunnerManager,
                    keyed_runner.RunnerNumber,
                    null));
                ts.Clock.Reset();
            }

            //Test case: an active session expired before its runners 
            //Arrange
            ts.ActSessOptions.RunnerIdleTimeout=TimeSpan.FromMinutes(3);
            using (ActiveSessionStore store = ts.CreateStore()) {
                session=store.FetchOrCreateSession(ts.MockSession.Object, null, null)??throw new Exception("Cannot create ActiveSession");
                session_cleanup_task=session.CleanupCompletionTask;
                ts.Clock.Advance(OwnCacheTestSetup.SESSION_ALMOST_GONE);
                Assert.Equal(1, store.GetCurrentStatistics()!.SessionCount);
                KeyedRunner<Result1> keyed_runner = store.CreateRunner<String, Result1>(
                    ts.MockSession.Object,
                    session,
                    (session as Active_Session)!.RunnerManager,
                    ID1,
                    null);
                Assert.Equal(1, store.GetCurrentStatistics()!.RunnerCount);
                SpyRunnerX runner = (SpyRunnerX)keyed_runner.Runner;
                Task disp_task = runner.DisposeTask;
                KeyedRunner<Result1> keyed_runner2 = store.CreateRunner<String, Result1>(
                    ts.MockSession.Object,
                    session,
                    (session as Active_Session)!.RunnerManager,
                    ID1,
                    null);
                Assert.Equal(2, store.GetCurrentStatistics()!.RunnerCount);
                SpyRunnerX runner2 = (SpyRunnerX)keyed_runner2.Runner;
                Task disp_task2 = runner2.DisposeTask;
                //Act
                ts.Clock.Advance(OwnCacheTestSetup.STEP);
                store.InitCacheExpiration();
                //Assess
                Assert.Equal(0, Task.WaitAny(session_cleanup_task, Task.Delay(10000)));
                Assert.Equal(0, store.GetCurrentStatistics()!.SessionCount);
                Assert.True((session as Active_Session)!.Disposed);
                Assert.Equal(0, Task.WaitAny(Task.WhenAll(disp_task,disp_task2), Task.Delay(10000)));
                stat=store.GetCurrentStatistics()!;
                Assert.Equal(0, stat.RunnerCount);
                Assert.True(runner.Disposed);
                Assert.Null(store.GetRunner(
                    ts.MockSession.Object,
                    session,
                    (session as Active_Session)!.RunnerManager,
                    keyed_runner.RunnerNumber,
                    null));
                Assert.True(runner2.Disposed);
                Assert.Null(store.GetRunner(
                    ts.MockSession.Object,
                    session,
                    (session as Active_Session)!.RunnerManager,
                    keyed_runner2.RunnerNumber,
                    null));
                ts.Clock.Reset();
            }

        }

        //Test group: CreateRunner with race condition
        [Fact]
        public void CreateRunner_Race()
        {
            Task session_cleanup_task;
            IActiveSession session;
            const string ID1 = "Id1";
            const string ID2 = "Id2";
            ManualResetEvent evt1=new ManualResetEvent(false), evt2=new ManualResetEvent(false);
            ManualResetEvent proceed_event = new ManualResetEvent(false);
            int test_value=0, test_factor=0;
            Boolean cr1 = false, cr2 = false;
            Action waiter1 = () => { cr1=true; proceed_event.Set(); evt1.WaitOne(); test_value+=test_factor; test_factor*=2; };
            Action waiter2 = () => { cr2=true; proceed_event.Set(); evt2.WaitOne(); test_value+=2*test_factor; test_factor*=2; };
            KeyedRunner<Result1> keyed_runner1=default;
            KeyedRunner<Result1> keyed_runner2 = default;
            Task create_task1, create_task2;
            Boolean global_lock_used;
            //Arrange common stuff
            Mock<ISession> stub_another_isession = new Mock<ISession>();
            stub_another_isession.SetupGet(s => s.Id).Returns("AnotherId");
            OwnCacheTestSetup ts = new OwnCacheTestSetup();

            //Test case: Create runner race conditions - ActiveSession level lock
            //Arrange
            ts.ActSessOptions.RunnerIdleTimeout=TimeSpan.FromMinutes(3);
            using (ActiveSessionStore store = ts.CreateStore()) {
                try {
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null)??throw new Exception("Cannot create ActiveSession");
                    session_cleanup_task=session.CleanupCompletionTask;
                    evt1.Reset();
                    evt2.Reset();
                    test_value=0;
                    test_factor=1;
                    cr1=false;
                    cr2=false;
                    //Act
                    ts.FactorySpyAction=waiter1;
                    proceed_event.Reset();
                    create_task1=Task.Run(()=>
                        {
                            keyed_runner1 = store.CreateRunner<String, Result1>(
                            ts.MockSession.Object,
                            session,
                            (session as Active_Session)!.RunnerManager,
                            ID1,
                            null);
                        }
                    );
                    Assert.True(proceed_event.WaitOne(10000));
                    ts.FactorySpyAction=waiter2;
                    proceed_event.Reset();
                    create_task2=Task.Run(() => {
                            keyed_runner2=store.CreateRunner<String, Result1>(
                                ts.MockSession.Object,
                                session,
                                (session as Active_Session)!.RunnerManager,
                                ID2,
                                null);
                        }
                    );
                    //Assess
                    Assert.True(cr1);
                    Assert.False(cr2);
                    global_lock_used=Task.WaitAny(Task.Run(()=>store.FetchOrCreateSession(stub_another_isession.Object,null, null)),Task.Delay(5000))!=0;
                    evt1.Set();
                    Assert.True(proceed_event.WaitOne(10000));
                    Assert.Equal(0, Task.WaitAny(create_task1, Task.Delay(2000)));
                    Assert.True(cr2);
                    evt2.Set();
                    Assert.Equal(0, Task.WaitAny(create_task2, Task.Delay(2000)));
                    //Next 2 lines tests runners creation order
                    Assert.Equal(4, test_factor);
                    Assert.Equal(5, test_value);
                    Assert.NotNull(keyed_runner1.Runner);
                    Assert.NotNull(keyed_runner2.Runner);
                    Assert.NotEqual(keyed_runner1.RunnerNumber, keyed_runner2.RunnerNumber);
                    Assert.False(global_lock_used);
                    //Cleanup
                    ts.Clock.Advance(OwnCacheTestSetup.STEP);
                    store.InitCacheExpiration();
                }
                finally {
                    ts.FactorySpyAction=null;
                    ts.Clock.Reset();
                }
            }

            //Test case: Create runner race conditions - store level lock
            //Arrange
            ts.ActSessOptions.RunnerIdleTimeout=TimeSpan.FromMinutes(3);
            using (ActiveSessionStore store = ts.CreateStore()) {
                try {
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null)??throw new Exception("Cannot create ActiveSession");
                    session_cleanup_task=session.CleanupCompletionTask;
                    evt1.Reset();
                    evt2.Reset();
                    test_value=0;
                    test_factor=1;
                    cr1=false;
                    cr2=false;
                    ts.MockRunnerManager.SetupGet(ts.LockObjectExpression).Returns((IRunnerManager?)null);
                    //Act
                    ts.FactorySpyAction=waiter1;
                    proceed_event.Reset();
                    create_task1=Task.Run(() =>
                        {
                            keyed_runner1=store.CreateRunner<String, Result1>(
                            ts.MockSession.Object,
                            session,
                            ts.MockRunnerManager.Object,
                            ID1,
                            null);
                        }
                    );
                    Assert.True(proceed_event.WaitOne(120000));
                    ts.FactorySpyAction=waiter2;
                    proceed_event.Reset();
                    create_task2=Task.Run(() => {
                        keyed_runner2=store.CreateRunner<String, Result1>(
                            ts.MockSession.Object,
                            session,
                            ts.MockRunnerManager.Object,
                            ID2,
                            null);
                    }
                    );
                    //Assess
                    Assert.True(cr1);
                    Assert.False(cr2);
                    global_lock_used=Task.WaitAny(Task.Run(() => store.FetchOrCreateSession(stub_another_isession.Object, null, null)), Task.Delay(2000))!=0;
                    evt1.Set();
                    Assert.True(proceed_event.WaitOne(120000));
                    Assert.Equal(0, Task.WaitAny(create_task1, Task.Delay(2000)));
                    Assert.True(cr2);
                    evt2.Set();
                    Assert.Equal(0, Task.WaitAny(create_task2, Task.Delay(2000)));
                    //Next 2 lines tests runners creation order
                    Assert.Equal(4, test_factor);
                    Assert.Equal(5, test_value);
                    Assert.NotNull(keyed_runner1.Runner);
                    Assert.NotNull(keyed_runner2.Runner);
                    Assert.True(global_lock_used);
                    //Cleanup
                    ts.Clock.Advance(OwnCacheTestSetup.STEP);
                    store.InitCacheExpiration();
                }
                finally {
                    ts.FactorySpyAction=null;
                    ts.Clock.Reset();
                }
            }

        }

        //Test group: IHostApplicationLifetime.ApplicationStopping signaled
        [Fact]
        public void ApplicationStopping() 
        {
            IActiveSession? session;
            using(ApplicationStoppingTestSetup ts = new ApplicationStoppingTestSetup()) {
                using(ActiveSessionStore store = ts.CreateStore()) {
                    //Termination of existing sessions
                    //Arrange
                    int dispose_count = 0;
                    ts.DisposeSpyAction = () => dispose_count++;
                    session=store.FetchOrCreateSession(ts.MockSession.Object, null, null);
                    Assert.NotNull(session);
                    Task cleanup_task = session.CleanupCompletionTask;
                    store.CreateRunner<String, Result1>(
                            ts.MockSession.Object,
                            session,
                            ((MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession)session).RunnerManager,
                            "Runner1",
                            null);
                    store.CreateRunner<String, Result1>(
                            ts.MockSession.Object,
                            session,
                            ((MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession)session).RunnerManager,
                            "Runner2",
                            null);
                    //Act
                    ts.Stop();
                    //Assess
                    Assert.True(cleanup_task.Wait(5000));
                    Assert.Equal(2, dispose_count);

                    //Attempt to create a session after signal
                    //Arrange
                    const String NEW_ID = "NewId";
                    Mock<ISession> extra_session_mock = new Mock<ISession>();
                    extra_session_mock.SetupGet(s => s.Id).Returns(NEW_ID);
                    //Act
                    session=store.FetchOrCreateSession(extra_session_mock.Object, null, null);
                    //Assess
                    Assert.Null(session);
                }
            }
        }

        //Test group: Disposing the store while having active sessions in the cache
        [Fact]
        public void Dispose_Store()
        {
            const Int32 TIMEOUT = 8000;
            const Int32 SMALL_TIMEOUT = 400;
            OwnCacheTestSetup ts = new OwnCacheTestSetup();
            ActiveSessionStore store;
            const String ID1 = "Session1";
            const String ID2 = "Session2";
            const String ID1A = "ActiveSession1";
            const String ID2A = "ActiveSession2";
            Mock<ISession> session1_mock = new Mock<ISession>();
            session1_mock.SetupGet(s => s.Id).Returns(ID1);
            Mock<ISession> session2_mock = new Mock<ISession>();
            session2_mock.SetupGet(s => s.Id).Returns(ID2);
            int dispose_count=0;
            ManualResetEventSlim pause_event = new ManualResetEventSlim(false);
            Action delay_disposal = () => { Task.Delay(SMALL_TIMEOUT).Wait(); dispose_count++; };
            Action pause_disposal = () => { pause_event.Wait(); dispose_count++; };
            Action instant_disposal = () => dispose_count++;
            Task dispose_task;
            MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession? as1, as2;
            Task cleanup_task1, cleanup_task2;
            //Test case: disposing in the case some sessions and runners haven't been completed within timeout
            //Arrange
            Arrange(true);
            //Act
            dispose_task=Task.Run(() => store.Dispose());
            //Assess
            Assess(true);
            //Test case: disposing in the case all sessions and runners have been completed within timeout
            //Arrange
            Arrange(false);
            //Act
            dispose_task=Task.Run(() => store.Dispose());
            //Assess
            Assess(false);
            //Test case: try to create a session in the disposed store
            Assert.Throws<ObjectDisposedException>(()=>store.FetchOrCreateSession(session1_mock.Object, null, null));

            void Arrange(Boolean Infinite)
            {
                ts.MockIdSupplier.Setup(s => s.GetBaseActiveSessionId(session1_mock.Object)).Returns(ID1A);
                ts.MockIdSupplier.Setup(s => s.GetBaseActiveSessionId(session2_mock.Object)).Returns(ID2A);
                dispose_count = 0;
                pause_event.Reset();
                store = ts.CreateStore();
                as1=store.FetchOrCreateSession(session1_mock.Object, null, null) as MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession;
                Assert.NotNull(as1);
                cleanup_task1=as1.CleanupCompletionTask;
                ts.DisposeSpyAction = delay_disposal;
                store.CreateRunner<String, Result1>(session1_mock.Object, as1, as1.RunnerManager,"Runner1a", null);
                ts.DisposeSpyAction = instant_disposal;
                store.CreateRunner<String, Result1>(session1_mock.Object, as1, as1.RunnerManager, "Runner1b", null);
                as2=store.FetchOrCreateSession(session2_mock.Object, null, null) as MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession;
                Assert.NotNull(as2);
                cleanup_task2=as2.CleanupCompletionTask;
                ts.DisposeSpyAction = delay_disposal;
                store.CreateRunner<String, Result1>(session2_mock.Object, as2, as2.RunnerManager, "Runner2a", null);
                ts.DisposeSpyAction = instant_disposal;
                if(Infinite) ts.DisposeSpyAction = pause_disposal;
                store.CreateRunner<String, Result1>(session2_mock.Object, as2, as2.RunnerManager, "Runner1b", null);
            }

            void Assess(Boolean Infinite)
            {
                Task.Delay(SMALL_TIMEOUT/2).Wait();
                Assert.False(cleanup_task1.IsCompleted);
                Assert.False(cleanup_task2.IsCompleted);
                if(Infinite) {
                    Assert.True(dispose_task.Wait(ActiveSessionStore.DISPOSE_TIMEOUT+TIMEOUT));
                    Assert.False(store._disposeNoTimedOut);
                    Assert.True(cleanup_task1.IsCompleted);
                    Assert.False(cleanup_task2.IsCompleted);
                    Assert.Equal(3, dispose_count);
                    pause_event.Set();
                    Assert.True(cleanup_task2.Wait(TIMEOUT));
                }
                else {
                    Assert.True(dispose_task.Wait(TIMEOUT));
                    Assert.True(store._disposeNoTimedOut);
                    Assert.True(cleanup_task1.IsCompleted);
                    Assert.True(cleanup_task2.IsCompleted);
                }
                Assert.Equal(4, dispose_count);
            }
        }

        //Test case: cleanup key-value pairs left behind by runners from outdated sessions
        [Fact]
        public void OutDatedRunnerSessionCleanup()
        {
            int number, gen;
            Active_Session session;
            String key90, key91, key10, key11;

            OwnCacheTestSetup ts = new OwnCacheTestSetup();
            var sess_mock = ts.MockSession;
            Mock<HttpContext> stub_context = new Mock<HttpContext>();

            using(ActiveSessionStore store = ts.CreateStore()) {
                gen=9;
                sess_mock.Object.SetInt32(SessionKey(),-(gen-1));
                session=(Active_Session?)store.FetchOrCreateSession(sess_mock.Object, null, null)??throw new Exception("Cannot create ActiveSession");
                Assert.Equal(gen, session.Generation);
                (_, number)=store.CreateRunner<String, Result1>(sess_mock.Object, session, session.RunnerManager, "", null );
                Assert.Equal(0, number);
                key90 = RunnerKey(number, gen);
                Assert.Equal(DEFAULT_HOST_NAME, sess_mock.Object.GetString(key90));
                Assert.Equal(typeof(Result1).FullName, sess_mock.Object.GetString(key90+"_Type"));
                (_, number)=store.CreateRunner<String, Result1>(sess_mock.Object, session, session.RunnerManager, "", null);
                Assert.Equal(1, number);
                key91 = RunnerKey(number, gen);
                Assert.Equal(DEFAULT_HOST_NAME, sess_mock.Object.GetString(key91));
                Assert.Equal(typeof(Result1).FullName, sess_mock.Object.GetString(key91+"_Type"));
                Assert.True(store.TerminateSession(sess_mock.Object, session, ts.MockRunnerManager.Object, null).Wait(5000));
                Assert.Equal(DEFAULT_HOST_NAME, sess_mock.Object.GetString(key90));
                Assert.Equal(typeof(Result1).FullName, sess_mock.Object.GetString(key90+"_Type"));
                Assert.Equal(DEFAULT_HOST_NAME, sess_mock.Object.GetString(key91));
                Assert.Equal(typeof(Result1).FullName, sess_mock.Object.GetString(key91+"_Type"));
                gen=11;
                sess_mock.Object.SetInt32(SessionKey(), -(gen-1));
                key10=RunnerKey(0, gen-1);
                sess_mock.Object.SetString(key10, DEFAULT_HOST_NAME);
                sess_mock.Object.SetString(key10+"_Type", typeof(Result1).FullName!);
                key11=RunnerKey(0, gen);
                sess_mock.Object.SetString(key11, DEFAULT_HOST_NAME);
                sess_mock.Object.SetString(key11+"_Type", typeof(Result1).FullName!);
                session=(Active_Session?)store.FetchOrCreateSession(sess_mock.Object, null, null)??throw new Exception("Cannot create ActiveSession");
                Assert.Equal(gen, session.Generation);
                Assert.Null(sess_mock.Object.GetString(key90));
                Assert.Null(sess_mock.Object.GetString(key90+"_Type"));
                Assert.Null(sess_mock.Object.GetString(key91));
                Assert.Null(sess_mock.Object.GetString(key91+"_Type"));
                Assert.Null(sess_mock.Object.GetString(key10));
                Assert.Null(sess_mock.Object.GetString(key10+"_Type"));
                Assert.Equal(DEFAULT_HOST_NAME, sess_mock.Object.GetString(key11));
                Assert.Equal(typeof(Result1).FullName, sess_mock.Object.GetString(key11+"_Type"));
            }
        }
        /////////////////////////////////////////////////////////////////////////////////////////////
        //ActiveSessionIdSupplier tests
        /////////////////////////////////////////////////////////////////////////////////////////////

        //Test group: test class
        [Fact]
        public void TestActiveSessionIdSupplier()
        {
            String? active_session_id = null;
            Byte[]? active_session_id_bytes = null;
            Mock<ISession> session_mock=new Mock<ISession>();
            session_mock.SetupGet(s => s.Id).Returns("SessionId");
            session_mock.Setup(s => s.TryGetValue(DEFAULT_SESSION_KEY_PREFIX, out active_session_id_bytes))
                .Callback((String _, out Byte[] data) => { data=active_session_id_bytes!; })
                .Returns(active_session_id_bytes==null);
            session_mock.Setup(s => s.Set(DEFAULT_SESSION_KEY_PREFIX, It.IsAny<Byte[]>()))
                .Callback((String _,Byte[] data) => { active_session_id_bytes=data; active_session_id=data==null ? null : Encoding.UTF8.GetString(data); });
            ActiveSessionOptions options=new ActiveSessionOptions();
            IOptions<ActiveSessionOptions> i_options = Options.Create(options);
            ActiveSessionIdSupplier supplier = new ActiveSessionIdSupplier(i_options);
            String id1,id2;
            //Test case: obtain a new Id
            id1=supplier.GetBaseActiveSessionId(session_mock.Object);
            Assert.NotEqual(session_mock.Object.Id,id1);
            Assert.NotNull(active_session_id_bytes);
            Assert.Equal(active_session_id, id1);
            Assert.Matches("[0-9A-Fa-f]{8}(-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12}", id1);
            //Test case: obtain the existing Id
            id2=supplier.GetBaseActiveSessionId(session_mock.Object);
            Assert.Equal(id1, id2);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        //Auxilary methods and clases
        /////////////////////////////////////////////////////////////////////////////////////////////

        private static String SessionKey()
        {
            return DEFAULT_SESSION_KEY_PREFIX+"_"+RunnerTestSetup.TEST_ACTIVESESSION_ID;
        }
        private static String RunnerKey()
        {
            return RunnerKey(RunnerTestSetup.RUNNER_1, RunnerTestSetup.TEST_GENERATION);
        }

        private static String RunnerKey(int RunnerNumber,int Generation)
        {
            return SessionKey() + "#" + Generation+"-"+RunnerNumber;
        }

        class FakeSystemClock : ISystemClock
        {
            DateTimeOffset _advancedTo,_advanceMoment;
            
            static  void DoReset(FakeSystemClock Item)
            {
                Item._advancedTo=Item._advanceMoment=DateTimeOffset.Now;
            }

            public FakeSystemClock()
            {
                DoReset(this);
            }

            public void Advance(TimeSpan Value)
            {
                _advanceMoment=DateTimeOffset.Now;
                _advancedTo=UtcNow+Value;
            }

            public void Reset() { DoReset(this); }

            public DateTimeOffset UtcNow { 
                get {
                    DateTimeOffset current = DateTimeOffset.Now;
                    if (current<_advancedTo) 
                        current = _advancedTo+(current-_advanceMoment)/2;
                    return current;
                }  
            } 
        }

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
            readonly Mock<IRunner<TResult>> _fakeRunner;
            readonly CancellationTokenSource _cts;

            public IRunner<TResult> Runner { get =>_fakeRunner.Object; }
            public readonly TRequest Arg;
            Expression<Action<IRunner<TResult>>> abort_expression= s => s.Abort(null);


            public MockedRunner(CancellationTokenSource Cts, TRequest Arg)
            {
                _cts=Cts;
                _fakeRunner=new Mock<IRunner<TResult>>();
                _fakeRunner.Setup(s => s.CompletionToken).Returns(_cts.Token);
                _fakeRunner.Setup(abort_expression);
                this.Arg=Arg;
            }

            public void VerifyAbort()
            {
                _fakeRunner.Verify(abort_expression, Times.AtLeastOnce);
            }
        }

        class ServiceProviderMock
        {
            readonly Mock<IServiceScopeFactory> _fakeScopeFactory;
            readonly Mock<IServiceScope> _fakeServiceScope;
            readonly Expression<Action<IServiceScope>> _disposeExpression = s => s.Dispose();

            readonly Mock<IServiceProvider> _fakeSessionServiceProvider;

            public Boolean ScopeDisposed { get; private set; }
            public Int32 ScopeDisposeCount { get; private set; } = 0;
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
                _fakeServiceScope.Setup(_disposeExpression).Callback(() => { ScopeDisposed=true; ScopeDisposeCount++; });

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
            public readonly Mock<IActiveSessionIdSupplier> MockIdSupplier;

            protected MockedLoggerFactory _loggerFactory;
            protected MockedLogger _logger;
            public IServiceProvider RootSP { get { return MockRootServiceProvider.Object; } }
            public Object LockObject { get => _lockObject; }
            public IHostApplicationLifetime HostAppLifetime { get => _stubAppLifetime.Object; }

            public readonly Mock<IServiceProvider> MockRootServiceProvider;
            protected readonly Mock<IHostApplicationLifetime> _stubAppLifetime;
            readonly Object _lockObject=new Object();

            public Expression<Func<IRunnerManager, Object?>> LockObjectExpression = s => s.RunnerCreationLock;

            public Boolean LoggerCreated() {
                return _loggerFactory.LoggerCreationCount(STORE_CATEGORY_NAME)>0;
            }

            public ConstructorTestSetup(Mock<IMemoryCache>? MockCache)
            {
                MockRootServiceProvider=new Mock<IServiceProvider>();
                _loggerFactory=new MockedLoggerFactory();
                _logger=_loggerFactory.MonitorLoggerCategory(SESSION_CATEGORY_NAME);
                this.MockCache=MockCache;
                IActSessionOptions=Options.Create(ActSessOptions);
                ISessOptions=Options.Create(SessOptions);
                MockRunnerManager=new Mock<IRunnerManager>();
                MockRunnerManager.SetupGet(LockObjectExpression).Returns(_lockObject);
                MockRunnerManager.Setup(s => s.PerformRunnersCleanupAsync(It.IsAny<IActiveSession>())).Returns(Task.CompletedTask);
                StubRMFactory=new Mock<IRunnerManagerFactory>();
                StubRMFactory.Setup(s => s.GetRunnerManager(It.IsAny<ILogger>(),
                    It.IsAny<IServiceProvider>(), It.IsAny<Int32>(), It.IsAny<Int32>())).Returns(MockRunnerManager.Object);
                _stubAppLifetime=new Mock<IHostApplicationLifetime>();
                MockIdSupplier= new Mock<IActiveSessionIdSupplier>();
            }

            public MockedLogger InitLogger(String CategoryName= STORE_CATEGORY_NAME)
            {
                _loggerFactory.ResetAllCategories();
                _logger=_loggerFactory.MonitorLoggerCategory(CategoryName);
                return _logger;
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
                    HostAppLifetime,
                    MockIdSupplier.Object,
                    _loggerFactory.LoggerFactory);
            }
        }

        class SessionAndRunnerBaseTestSetup : ConstructorTestSetup
        {
            public const String TEST_SESSION_ID = "TestSessionId";
            public const String TEST_ACTIVESESSION_ID = "TestActiveSessionId";

            public readonly Mock<ISession> MockSession;
            public readonly Expression<Action<ISession>> SessionKeyRemoveExpression = s => s.Remove(It.IsAny<String>());

            public Dictionary<String, byte[]> _session_values = new Dictionary<String, byte[]>();


            protected SessionAndRunnerBaseTestSetup(Mock<IMemoryCache>? MockCache) : base(MockCache)
            {
                MockSession=new Mock<ISession>();
                MockSession.SetupGet(s => s.IsAvailable).Returns(true);
                MockSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                MockSession.Setup(SessionKeyRemoveExpression)
                    .Callback((String key) => { _session_values.Remove(key); });
                MockSession.Setup(s => s.Set(It.IsAny<String>(), It.IsAny<byte[]>()))
                    .Callback((String key, byte[] value) => {
                        if (_session_values.ContainsKey(key))
                            _session_values[key]=value;
                        else
                            _session_values.Add(key, value);
                    });
                MockSession.Setup(s => s.TryGetValue(It.IsAny<String>(), out It.Ref<Byte[]?>.IsAny))
                    .Returns((String key, out byte[]? value) => {
                        if (_session_values.ContainsKey(key)) {
                            value=_session_values[key];
                            return true;
                        }
                        else {
                            value=null;
                            return false;
                        }
                    });
                MockSession.SetupGet(s => s.Keys).Returns(_session_values.Keys);
                MockIdSupplier.Setup(s => s.GetBaseActiveSessionId(MockSession.Object)).Returns(TEST_ACTIVESESSION_ID);
            }

            static IRunnerFactory<TRequest, TResult> MockRunnerFactory<TRequest, TResult>(
                Func<TRequest, IRunner<TResult>> Factory)
            {
                Mock<IRunnerFactory<TRequest, TResult>>? factory_mock = new();
                factory_mock.Setup(s => s.Create(It.IsAny<TRequest>(), It.IsAny<IServiceProvider>(), It.IsAny<RunnerId>(), It.IsAny<String>()))
                    .Returns((TRequest r, IServiceProvider _, RunnerId ri, String ti) => Factory(r));
                return factory_mock.Object;
            }

            public void AddRunnerFactory<TRequest, TResult>(Func<TRequest, IRunner<TResult>> Factory)
            {
                MockRootServiceProvider.Setup(RunnerFactoryExpression<TRequest, TResult>())
                    .Returns(MockRunnerFactory(Factory));
            }

            public Expression<Func<IServiceProvider, Object?>> RunnerFactoryExpression<TRequest, TResult>()
            {
                return s => s.GetService(typeof(IRunnerFactory<TRequest, TResult>));
            }

        }

        class CachedSessionAndRunnerBaseTestSetup : SessionAndRunnerBaseTestSetup
        {
            public readonly MockedCache Cache;

            protected CachedSessionAndRunnerBaseTestSetup(MockedCache Cache) : base(Cache.CacheMock)
            {
                this.Cache=Cache;
            }
        }


        class CreateFetchTestSetup : CachedSessionAndRunnerBaseTestSetup, IDisposable
        {
            public Boolean ScopeDisposed { get { return _mockedSessionServiceProvider.ScopeDisposed; } }
            public Int32 ScopeDisposeCount { get { return _mockedSessionServiceProvider.ScopeDisposeCount; } }
            public IServiceProvider ScopeServiceProvider { get { return _mockedSessionServiceProvider.ScopeServiceProvider; } }
            public readonly Expression<Action<IRunnerManager>> RegisterSessionExpression = (s => s.RegisterSession(It.IsAny<IActiveSession>()));
            public readonly Expression<Action<IRunnerManager>> AbortAllExpression = s => s.AbortAll(It.IsAny<IActiveSession>());
            public readonly Expression<Func<IRunnerManager, Task>> PerformRunnersCleanupExpression =
                s => s.PerformRunnersCleanupAsync(It.IsAny<IActiveSession>());

            readonly CancellationTokenSource _cts;
            readonly ServiceProviderMock _mockedSessionServiceProvider;
            Action? _callback;


            public CreateFetchTestSetup() : base(new MockedCache())
            {
                _mockedSessionServiceProvider=new ServiceProviderMock(MockRootServiceProvider);
                _cts=new CancellationTokenSource();
                MockRunnerManager.Setup(RegisterSessionExpression);
                MockRunnerManager.Setup(AbortAllExpression);
            }

            public void Dispose()
            {
                _cts.Dispose();
            }

            public void SetCleanupCallback(Action? Callback)
            {
                _callback=Callback;
                MockRunnerManager.Setup(PerformRunnersCleanupExpression).Returns(_callback==null ? Task.CompletedTask : Task.Run(_callback));
            }
        }

        class RunnerTestSetup : CachedSessionAndRunnerBaseTestSetup, IDisposable
        {
            public readonly Mock<IActiveSession> StubActiveSession;
            readonly Object? _lockObject = null;
            readonly CancellationTokenSource _cts;

            public readonly Expression<Func<IRunnerManager, Int32>> GetRunnerNumberExpression;
            public readonly Expression<Action<IRunnerManager>> ReturnRunnerNumberExpression;
            public readonly Expression<Action<IRunnerManager>> RegisterRunnerExpression;
            public readonly Expression<Action<IRunnerManager>> UnregisterRunnerExpression;
            public readonly Expression<Func<IRunnerManager,Object?>> RunnerCreationLockExpression = (s => s.RunnerCreationLock);

            public const Int32 RUNNER_1 = 1;
            public const Int32 TEST_GENERATION = 2;

            public Action? CreateStage1Callback { get; set; }
            public Action? CreateStage3Callback { get; set; }
            public Action? CreateStage4Callback { get; set; }

            public RunnerTestSetup(Boolean PerSessionLock=true) : base(new MockedCache()) 
            {
                MockSession.Setup(s => s.Set(It.IsAny<String>(), It.IsAny<byte[]>()))
                    .Callback((String key, byte[] value) => {
                        CreateStage3Callback?.Invoke();
                        if (_session_values.ContainsKey(key)) _session_values[key] = value; 
                        else _session_values.Add(key, value); 
                    });
                StubActiveSession=new Mock<IActiveSession>();
                _cts=new CancellationTokenSource();
                StubActiveSession.SetupGet(s => s.CompletionToken).Returns(_cts.Token);
                StubActiveSession.SetupGet(s => s.Id).Returns(TEST_ACTIVESESSION_ID);
                StubActiveSession.SetupGet(s => s.Generation).Returns(TEST_GENERATION);
                if (PerSessionLock) _lockObject=new Object();
                GetRunnerNumberExpression=(s => s.GetNewRunnerNumber(StubActiveSession.Object, It.IsAny<String>()));
                ReturnRunnerNumberExpression=(s => s.ReturnRunnerNumber(StubActiveSession.Object, It.IsAny<Int32>()));
                RegisterRunnerExpression=(s => s.RegisterRunner(StubActiveSession.Object, It.IsAny<Int32>(), It.IsAny<IRunner>(),It.IsAny<Type>(),It.IsAny<String>()));
                UnregisterRunnerExpression=(s => s.UnregisterRunner(StubActiveSession.Object, It.IsAny<Int32>()));
                MockRunnerManager.Setup(GetRunnerNumberExpression)
                    .Callback((IActiveSession _, String _) => { CreateStage1Callback?.Invoke(); })
                    .Returns(RUNNER_1);
                MockRunnerManager.Setup(ReturnRunnerNumberExpression);
                MockRunnerManager.Setup(RegisterRunnerExpression)
                    .Callback((IActiveSession _,Int32 _,IRunner _, Type _, String _) => { CreateStage4Callback?.Invoke(); });
                MockRunnerManager.Setup(UnregisterRunnerExpression);
                MockRunnerManager.SetupGet(RunnerCreationLockExpression).Returns(_lockObject);
            }

            public void Dispose()
            {
                _cts.Dispose();
            }

        }

        class OwnCacheTestSetup: SessionAndRunnerBaseTestSetup
        {
            public readonly FakeSystemClock Clock;

            public static readonly TimeSpan STEP = TimeSpan.FromMinutes(2) ;
            public static readonly TimeSpan SESSION_IDLE = TimeSpan.FromMinutes(10); //Default was 20 min
            public static readonly TimeSpan SESSION_ALMOST_GONE = SESSION_IDLE-TimeSpan.FromSeconds(20);
            public static readonly TimeSpan RUNNER_IDLE = TimeSpan.FromSeconds(30); //Default
            public Action? DisposeSpyAction = null;
            public Action? FactorySpyAction = null;

            readonly ServiceProviderMock _mockedSessionServiceProvider;
            RunnerManagerFactory _runnerManagerFactory;

            public OwnCacheTestSetup(): base(null) 
            {
                _mockedSessionServiceProvider=new ServiceProviderMock(MockRootServiceProvider);
                SessOptions.IdleTimeout=SESSION_IDLE; 
                ActSessOptions.UseOwnCache=true;
                ActSessOptions.TrackStatistics=true;
                Clock=new FakeSystemClock();
                ActSessOptions.OwnCacheOptions=new MemoryCacheOptions { 
                    Clock=Clock, 
                    ExpirationScanFrequency=TimeSpan.FromSeconds(10) 
                };
                _runnerManagerFactory=new RunnerManagerFactory(IActSessionOptions);
                AddRunnerFactory<String, Result1>(SpyRunnerFactory);
            }

            IRunner<Result1> SpyRunnerFactory(String Request)
            {
                FactorySpyAction?.Invoke();
                return new SpyRunnerX(Request,DisposeSpyAction);
            }

            public new ActiveSessionStore CreateStore()
            {
                IMemoryCache? cache = MockCache?.Object;
                return new ActiveSessionStore(
                    null,
                    MockRootServiceProvider.Object,
                    _runnerManagerFactory,
                    IActSessionOptions,
                    ISessOptions, 
                    HostAppLifetime,
                    MockIdSupplier.Object,
                    _loggerFactory.LoggerFactory);
            }

        }

        class ApplicationStoppingTestSetup: OwnCacheTestSetup, IDisposable
        {
            CancellationTokenSource _cts;
            public ApplicationStoppingTestSetup():base() 
            {
                _cts=new CancellationTokenSource();
                _stubAppLifetime.SetupGet(s => s.ApplicationStopping).Returns(_cts.Token);
            }

            public void Dispose()
            {
                _cts.Dispose();
            }

            public void  Stop()
            {
                _cts.Cancel();
            }
        }

        public class SpyRunnerX : RunnerBase, IRunner<Result1>
        {
            public new Boolean Disposed { get=>base.Disposed();}

            public String Arg { get; private set; }

            public override Boolean IsBackgroundExecutionCompleted => throw new NotImplementedException();

            Action? _disposeSpyAction;
            ManualResetEvent _evt = new ManualResetEvent(false);
            public Task DisposeTask;

            public SpyRunnerX(String Arg, Action? DisposeSpyAction):base(null, true, default)  
            {
                this.Arg=Arg;
                DisposeTask=new Task(() => { _evt.WaitOne(); _evt.Dispose(); });
                _disposeSpyAction=DisposeSpyAction;
            }

            protected override void Dispose(Boolean Disposing)
            {
                DisposeTask.Start();
                _disposeSpyAction?.Invoke();
                _evt.Set();
                base.Dispose(Disposing);
            }

            public ValueTask<RunnerResult<Result1>> GetRequiredAsync(
                Int32 Advance = IRunner.DEFAULT_ADVANCE,
                CancellationToken Token = default,
                Int32 StartPosition = IRunner.CURRENT_POSITION,
                String? TraceIdentifier = null)
            {
                throw new NotImplementedException();
            }

            public RunnerResult<Result1> GetAvailable(Int32 Advance = int.MaxValue, Int32 StartPosition = -1, String? TraceIdentifier = null)
            {
                throw new NotImplementedException();
            }

            protected internal override void StartBackgroundExecution()
            {
                throw new NotImplementedException();
            }

            public override RunnerBkgProgress GetProgress()
            {
                throw new NotImplementedException();
            }
        }


    }
}
