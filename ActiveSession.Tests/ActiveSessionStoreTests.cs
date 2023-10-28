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


namespace ActiveSession.Tests
{
    public class ActiveSessionStoreTests
    {
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
                ts.MockLogerFactory.Verify(ts.LoggerCreateExpression, Times.Once);
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
        public void CreateOrFetchSession_MockedCache()
        {
            CreateFetchTestSetup ts;
            ActiveSessionStore store;
            IActiveSession? session;
            IRunnerManager? manager;

            using (ts=new CreateFetchTestSetup()) {
                using (store=ts.CreateStore()) {

                    //Test case: create new ActiveSession
                    session = store.FetchOrCreateSession(ts.StubSession.Object, null);

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
                    Assert.Equal(ts.ScopeServiceProvider, manager.Services);
                    //Assess a cache entry
                    ts.Cache.CacheMock.Verify(MockedCache.TryGetValueExpression, Times.Exactly(2));//2-nd time - after obtainning lock. Fragile!!! 
                    ts.Cache.CacheMock.Verify(MockedCache.CreateEntryEnpression, Times.Once);
                    Assert.True(ts.Cache.IsEntryStored);
                    Assert.Equal(DEFAULT_SESSION_KEY_PREFIX+"_"+CreateFetchTestSetup.TEST_SESSION_ID,ts.Cache.Key);
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

                    //Test case: dispoing ActiveSession while in a cache object w/o runners associated
                    (session as IDisposable)?.Dispose();
                    //Due to the cache is moked and we have no active runners here the cache eviction is to be synchronous
                    Assert.False(ts.Cache.IsEntryStored);
                    Assert.Equal(1,ts.Cache.CalledCallbacksCount);

                }
                //Test case: options passed to ActiveSessionStore constructor affects cache entry
                TimeSpan EXPIRATION = TimeSpan.FromMinutes(1);
                TimeSpan MAX_LIFETIME = TimeSpan.FromHours(1);
                String PREFIX = "TestPrefix";
                Int32 AS_SIZE = 1;

                ts.SessOptions.IdleTimeout=EXPIRATION;
                ts.ActSessOptions.MaxLifetime=MAX_LIFETIME;
                ts.ActSessOptions.Prefix=PREFIX;
                ts.ActSessOptions.TrackStatistics=true;
                ts.ActSessOptions.WaitForEvictedSessionDisposal=true;
                using (store=ts.CreateStore()) {

                    session=store.FetchOrCreateSession(ts.StubSession.Object, null);

                    //Assess
                    Assert.NotNull(session);
                    Assert.True(ts.Cache.IsEntryStored);
                    Assert.Equal(PREFIX+"_"+CreateFetchTestSetup.TEST_SESSION_ID, ts.Cache.Key);
                    Assert.Equal(EXPIRATION, ts.Cache.SlidingExpiration);
                    Assert.Equal(MAX_LIFETIME, ts.Cache.AbsoluteExpirationRelativeToNow);
                    Assert.Equal(AS_SIZE, store.GetCurrentStatistics()!.StoreSize);

                    //Test case: removing ActiveSession from cache
                    ts.Cache.CacheMock.Object.Remove(PREFIX+"_"+CreateFetchTestSetup.TEST_SESSION_ID);
                    Assert.Equal(0, store.GetCurrentStatistics()!.StoreSize);
                    Assert.False(ts.Cache.IsEntryStored);
                    Assert.Equal(1, ts.Cache.CalledCallbacksCount);
                    Assert.True((session as Active_Session)!.Disposed);

                    //Test case: race condition in FetchOrCreateSession method
                    //TODO
                    //Test case: dispoing ActiveSession asynchronously while in a cache object w/o runners associated
                }
            }
                ;

        }

        class MockedCache
        {
            public readonly Mock<IMemoryCache> CacheMock;
            public readonly Mock<ICacheEntry> EntryMock;
            public static readonly Expression<Func<IMemoryCache, ICacheEntry>> CreateEntryEnpression = s => s.CreateEntry(It.IsAny<Object>());
            public static readonly Expression<Func<IMemoryCache, Boolean>> TryGetValueExpression =
                s => s.TryGetValue(It.IsAny<Object>(), out It.Ref<Object>.IsAny);

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
                    .Callback((Object key) => { ClearStoredEntry(); _key=key; })
                    .Returns(EntryMock.Object);
                CacheMock.Setup(TryGetValueExpression)
                    .Callback((Object _, ref Object value) => { value=_value!; })
                    .Returns((Object key, ref Object _ ) => _isEntryStored&&key.Equals(_key));
                CacheMock.Setup(s => s.Remove(It.IsAny<Object>()))
                    .Callback((Object Key) => { CheckKey(Key); Evict(EvictionReason.Removed); });

                EntryMock.Setup(s => s.Dispose())
                    .Callback(() => { if (!_isEntryStored&&EntryMock.Object.Value!=null) StoreEntry();});
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
            public readonly Mock<ILoggerFactory> MockLogerFactory;
            public readonly Mock<IRunnerManagerFactory> StubRMFactory;
            public readonly Mock<IRunnerManager> MockRunnerManager;
            public Expression<Func<ILoggerFactory, ILogger>> LoggerCreateExpression = s => s.CreateLogger(LOGGING_CATEGORY_NAME);
            public IServiceProvider RootSP { get { return _fakeRootServiceProvider.Object; } }

            protected readonly Mock<IServiceProvider> _fakeRootServiceProvider;
            readonly Object _lockObject=new Object();

            public ConstructorTestSetup(Mock<IMemoryCache>? MockCache)
            {
                _fakeRootServiceProvider=new Mock<IServiceProvider>();
                MockLogerFactory = new Mock<ILoggerFactory>();
                MockLogerFactory.Setup(LoggerCreateExpression).Returns<ILogger>(null);
                this.MockCache=MockCache;
                IActSessionOptions=Options.Create(ActSessOptions);
                ISessOptions=Options.Create(SessOptions);
                MockRunnerManager=new Mock<IRunnerManager>();
                MockRunnerManager.SetupGet(s => s.RunnerCreationLock).Returns(_lockObject);
                StubRMFactory=new Mock<IRunnerManagerFactory>();
                StubRMFactory.Setup(s => s.GetRunnerManager(It.IsAny<String>(), It.IsAny<ILogger>(),
                    It.IsAny<IServiceProvider>(), It.IsAny<Int32>(), It.IsAny<Int32>())).Returns(MockRunnerManager.Object);
            }

            public ActiveSessionStore CreateStore()
            {
                IMemoryCache? cache = MockCache?.Object;
                return new ActiveSessionStore(
                    cache,
                    _fakeRootServiceProvider.Object,
                    StubRMFactory.Object, 
                    IActSessionOptions,
                    ISessOptions,
                    MockLogerFactory.Object);
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
            public readonly Mock<ISession> StubSession;
            public Boolean ScopeDisposed { get; private set; }
            public IServiceProvider ScopeServiceProvider { 
                get {
                    if (ScopeDisposed) throw new ObjectDisposedException("IServiceScope");
                    return _fakeSessionServiceProvider.Object; 
                } 
            }

            protected  readonly Mock<IServiceProvider> _fakeSessionServiceProvider;

            readonly Mock<IServiceScopeFactory> _fakeScopeFactory;
            readonly Mock<IServiceScope> _fakeServiceScope;
            readonly Expression<Action<IServiceScope>> _disposeExpression = s=>s.Dispose();
            readonly CancellationTokenSource _cts;


            public CreateFetchTestSetup() : base(new MockedCache())
            {
                StubSession=new Mock<ISession>();
                StubSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                _fakeSessionServiceProvider = new Mock<IServiceProvider>();
                _fakeServiceScope=new Mock<IServiceScope>();
                _fakeServiceScope.SetupGet(s => s.ServiceProvider).Returns(ScopeServiceProvider); 
                _fakeServiceScope.Setup(_disposeExpression).Callback(() => { ScopeDisposed=true; });
                _fakeScopeFactory=new Mock<IServiceScopeFactory>();
                _fakeScopeFactory.Setup(s => s.CreateScope())
                    .Callback(() => ScopeDisposed=false)
                    .Returns(_fakeServiceScope.Object);
                _fakeRootServiceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
                    .Returns(_fakeScopeFactory.Object);
                _cts=new CancellationTokenSource();
                MockRunnerManager.SetupGet(s => s.SessionCompletionToken).Returns(_cts.Token);
                MockRunnerManager.SetupGet(s => s.Services).Returns(_fakeSessionServiceProvider.Object);
            }

            public void Dispose()
            {
                _cts.Dispose();
            }
        }

        class RunnerTestSetup: MockedCaheTestSetup
        {
            public readonly Mock<ISession> StubSession;

            public Expression<Func<IRunnerManager, Int32>> GetRunnerNumberExpression = (s => s.GetNewRunnerNumber(It.IsAny<String>()));
            public Expression<Action<IRunnerManager>> ReturnRunnerNumberExpression = (s => s.ReturnRunnerNumber(RUNNER_1));
            public Expression<Action<IRunnerManager>> RegisterRunnerExpression = (s => s.RegisterRunner(RUNNER_1));
            public Expression<Action<IRunnerManager>> UnregisterRunnerExpression = (s => s.UnregisterRunner(RUNNER_1));
            public Expression<Func<IRunnerManager, IServiceProvider>> ServicesExpression = (s => s.Services);
            public Expression<Func<IRunnerManager,Object?>> RunnerCreationLockExpression = (s => s.RunnerCreationLock);
            public Expression<Func<IRunnerManager, CancellationToken>> SessionCompletionTokenExpression = (s=>s.SessionCompletionToken);

            public const Int32 RUNNER_1 = 1;


            public RunnerTestSetup() : base(new MockedCache()) 
            {
                StubSession=new Mock<ISession>();
                StubSession.SetupGet(s => s.Id).Returns(TEST_SESSION_ID);
                MockRunnerManager.Setup(GetRunnerNumberExpression).Returns(RUNNER_1);
                MockRunnerManager.Setup(ReturnRunnerNumberExpression);
                MockRunnerManager.Setup(RegisterRunnerExpression);
                MockRunnerManager.Setup(UnregisterRunnerExpression);
                MockRunnerManager.Setup(ServicesExpression);
                MockRunnerManager.Setup(RunnerCreationLockExpression);
                MockRunnerManager.Setup(SessionCompletionTokenExpression);
                //TODO Setup properties and methods

            }
        }

    }
}
