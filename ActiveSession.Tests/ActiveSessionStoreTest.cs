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

namespace ActiveSession.Tests
{
    public class ActiveSessionStoreTest
    {
        [Fact]
        public void CreateActiveSessionStore() 
        {
            ConstructorTestSetup ts;
            Mock<IMemoryCache> dummy_cache=new Mock<IMemoryCache>();
            ts=new ConstructorTestSetup(dummy_cache);

            //Test case: null IMemoryCahe argument while own caches is not used
            Assert.Throws<InvalidOperationException>(()=>new ActiveSessionStore(
                null, ts.SP, ts.IActSessionOptions, ts.ISessOptions));

            //Test case: null IServiceProvider argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, null!, ts.IActSessionOptions, ts.ISessOptions));

            //Test case: null IOptions<ActiveSessionOptions> argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, ts.SP, null!, ts.ISessOptions));

            //Test case: null IOptions<SessionOptions> argument
            Assert.Throws<ArgumentNullException>(() => new ActiveSessionStore(
                dummy_cache.Object, ts.SP, ts.IActSessionOptions, null!));

            //Test case: using shared cache
            using (ts.CreateStore()) {
                ts.MockLogerFactory.Verify(ts.LoggerCreateExpression, Times.Once);
            }
        }

        [Fact]
        public void GetCurrentStatisticsTest()
        {
            ConstructorTestSetup ts;
            Mock<IMemoryCache> dummy_cache = new Mock<IMemoryCache>();
            ActiveSessionStore store;
            ActiveSessionStoreStats? stats;
            ts =new ConstructorTestSetup(dummy_cache);

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

        class ConstructorTestSetup
        {
            public ActiveSessionOptions ActSessOptions = new ActiveSessionOptions();
            public SessionOptions SessOptions = new SessionOptions();
            public readonly IOptions<ActiveSessionOptions> IActSessionOptions;
            public readonly IOptions<SessionOptions> ISessOptions;
            public readonly Mock<IMemoryCache>? MockCache;
            public readonly Mock<ILoggerFactory> MockLogerFactory;
            public Expression<Func<ILoggerFactory, ILogger>> LoggerCreateExpression = s => s.CreateLogger(LOGGING_CATEGORY_NAME);
            public IServiceProvider SP { get { return _fakeServiceProvider.Object; } }

            protected readonly Mock<IServiceProvider> _fakeServiceProvider;

            public ConstructorTestSetup(Mock<IMemoryCache>? MockCache)
            {
                _fakeServiceProvider=new Mock<IServiceProvider>();
                MockLogerFactory = new Mock<ILoggerFactory>();
                MockLogerFactory.Setup(LoggerCreateExpression).Returns<ILogger>(null);
                this.MockCache=MockCache;
                IActSessionOptions=Options.Create(ActSessOptions);
                ISessOptions=Options.Create(SessOptions);
            }

            public ActiveSessionStore CreateStore()
            {
                IMemoryCache? cache = MockCache?.Object;
                return new ActiveSessionStore(
                    cache,
                    _fakeServiceProvider.Object,
                    IActSessionOptions,
                    ISessOptions,
                    MockLogerFactory.Object);
            }
        }
    }
}
