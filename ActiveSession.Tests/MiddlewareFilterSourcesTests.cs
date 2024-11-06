using Microsoft.AspNetCore.Http;
using MVVrus.AspNetCore.ActiveSession.Internal;

namespace ActiveSession.Tests
{
    public class MiddlewareFilterSourcesTests
    {
        const Int32 DEFAULT_ORDER = 42;
        const String PATH1 = "/path1", PATH2 = "/path2";

        //SimplePredicateFilterSource
        [Fact]
        public void SimplePredicateFilterSource()
        {
            Boolean was_mapped;
            String? session_suffix;
            Int32 order;

            Func<HttpContext,Boolean> predicate= context => { return context.Request.Path.StartsWithSegments(PATH1); };
            var source = new SimplePredicateFilterSource(predicate);
            var filter = source.Create(DEFAULT_ORDER);
            FakeHttpContext fake_context = new FakeHttpContext();
            Assert.NotNull(filter);
            Assert.Equal(DEFAULT_ORDER, filter.MinOrder);
            fake_context.SetPath(PATH1);
            (was_mapped, session_suffix, order)=filter.Apply(fake_context.Context);
            Assert.True(was_mapped);
            Assert.Null(session_suffix);
            Assert.Equal(DEFAULT_ORDER, order);

            fake_context.SetPath(PATH2);
            (was_mapped, session_suffix, order)=filter.Apply(fake_context.Context);
            Assert.False(was_mapped);
            Assert.Null(session_suffix);
            Assert.Equal(DEFAULT_ORDER, order);
        }

        const String SUFFIX1 = "1";

        [Fact]
        public void PredicateWithSuffixFilterSource()
        {
            Boolean was_mapped;
            String? session_suffix;
            Int32 order;

            Func<HttpContext, Boolean> predicate = context => { return context.Request.Path.StartsWithSegments(PATH1); };
            var source = new PredicateWithSuffixFilterSource(predicate,SUFFIX1);
            var filter = source.Create(DEFAULT_ORDER);
            FakeHttpContext fake_context = new FakeHttpContext();
            Assert.NotNull(filter);
            Assert.Equal(DEFAULT_ORDER, filter.MinOrder);
            fake_context.SetPath(PATH1);
            (was_mapped, session_suffix, order)=filter.Apply(fake_context.Context);
            Assert.True(was_mapped);
            Assert.Equal(SUFFIX1,session_suffix);
            Assert.Equal(DEFAULT_ORDER, order);

            fake_context.SetPath(PATH2);
            (was_mapped, session_suffix, order)=filter.Apply(fake_context.Context);
            Assert.False(was_mapped);
            Assert.Null(session_suffix);
            Assert.Equal(DEFAULT_ORDER, order);
        }

        class FakeHttpContext
        {
            public HttpContext Context { get => _mockContext.Object; }
            Mock<HttpContext> _mockContext { get; init; }
            String? _path = null;

            public FakeHttpContext()
            {
                _mockContext=new Mock<HttpContext>();
                _mockContext.SetupGet(s => s.Request.Path).Returns(() => _path);
            }

            public void SetPath(String Path)
            {
                _path=Path;
            }
        }


    }
}
