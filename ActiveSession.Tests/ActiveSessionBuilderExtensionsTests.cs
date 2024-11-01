using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ActiveSessionBuilderExtensionsTests
    {
        class TestSetup
        {
            Mock<IActiveSessionStore> dummy_store;
            public Mock<IServiceProvider> stub_sp;
            MockedLoggerFactory factory_mock;
            public Mock<IApplicationBuilder> mock_builder;
            Func<RequestDelegate, RequestDelegate>? use_delegate;
            IDictionary<String, Object?> _properties;

            public TestSetup(Boolean AddInfrastructureServices = true)
            {
                dummy_store=new Mock<IActiveSessionStore>();
                stub_sp = new Mock<IServiceProvider>();
                if(AddInfrastructureServices) stub_sp.Setup(s => s.GetService(typeof(IActiveSessionStore))).Returns(dummy_store.Object);
                factory_mock = new MockedLoggerFactory();
                stub_sp.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(factory_mock.LoggerFactory);
                mock_builder = new Mock<IApplicationBuilder>();
                mock_builder.SetupGet(s => s.ApplicationServices).Returns(stub_sp.Object);
                _properties=new Dictionary<String, Object?>();
                mock_builder.SetupGet(s => s.Properties).Returns(_properties);
                use_delegate = null;
                mock_builder.Setup(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                        .Callback((Func<RequestDelegate, RequestDelegate> f) => use_delegate=f).Returns(mock_builder.Object);
            }

            public ActiveSessionMiddleware.MiddlewareParam VerifyAndGetMidddewareParam()
            {
                //The tests below may be fragile because of use of the specific knowledge about UseMiddleware method implementation
                //Test type of a middleware to be installed
                Assert.NotNull(use_delegate);
                Object? use_delegate_target = use_delegate!.Target;
                Assert.NotNull(use_delegate_target);
                Type delegate_target_type = use_delegate_target.GetType();
                FieldInfo? middleware_field = delegate_target_type.GetField("middleware");
                Assert.NotNull(middleware_field);
                Type? middleware = (Type?)(middleware_field!.GetValue(use_delegate_target));
                Assert.Equal(typeof(ActiveSessionMiddleware), middleware);
                //Test arguments used for the middleware intallation
                FieldInfo? args_field = delegate_target_type.GetField("args");
                Assert.NotNull(args_field);
                Object[]? arg_values = (Object[]?)(args_field!.GetValue(use_delegate_target));
                Assert.NotNull(arg_values);
                Assert.Single(arg_values);
                Assert.IsType<ActiveSessionMiddleware.MiddlewareParam>(arg_values[0]);
                return (ActiveSessionMiddleware.MiddlewareParam)arg_values[0];
            }

        }

        //Test case: use UseActiveSessions with required services added to the service container and default filter
        [Fact]
        public void UseActiveSessions_ServicesAdded()
        {
            //Arrange
            TestSetup ts=new TestSetup();
            //Act
            ts.mock_builder.Object.UseActiveSessions();
            //Assess
            ts.mock_builder.Verify(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()),Times.Once);
            ActiveSessionMiddleware.MiddlewareParam middleware_param = ts.VerifyAndGetMidddewareParam();
            Assert.True(middleware_param.AcceptAll);
        }

        //Test case: use UseActiveSessions with required services not added to the service container
        [Fact]
        public void UseActiveSessions_ServicesNotAdded()
        {
            //Arrange
            TestSetup ts = new TestSetup(false);
            //Act
            ts.mock_builder.Object.UseActiveSessions();
            //Assess
           ts.mock_builder.Verify(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Never);
        }

        //Test case: custom filter delegate installation
        [Fact]
        public void UseActiveSessions_DelegateFilter()
        {
            //Arrange
            TestSetup ts = new TestSetup();
            Func<HttpContext,Boolean> filter= context => context.Request.Path.Equals("/");
            //Act
            ts.mock_builder.Object.UseActiveSessions(filter);
            //Assess
            ActiveSessionMiddleware.MiddlewareParam middleware_param = ts.VerifyAndGetMidddewareParam();
            Assert.False(middleware_param.AcceptAll);
            Assert.Single(middleware_param.Filters);
            Assert.True(ReferenceEquals(filter, (middleware_param.Filters[0] as SimplePredicateFilterSource)?.Predicate));
        }

        //Test case: Regex-based filter delegate installation
        [Fact]
        public void UseActiveSessions_RegexFilter()
        {
            //Arrange
            const string PATH1 = "path1";
            const string PATH2 = "path2";
            const string REST = "(/subpath?p1=1";
            const string REQ_PATH1 = "/"+PATH1+REST;
            const string REQ_PATH2 = "/"+PATH2+REST;
            ActiveSessionOptions options = new ActiveSessionOptions();

            TestSetup ts = new TestSetup();
            ts.stub_sp.Setup(s => s.GetService(typeof(IOptions<ActiveSessionOptions>))).Returns(Options.Create(options));
            Mock<HttpContext> fake_context=new Mock<HttpContext>();
            fake_context.SetupSequence(s => s.Request.Path).Returns(REQ_PATH1).Returns(REQ_PATH2);
            String filter="^/"+PATH1+"(/.*)?";
            //Act
            ts.mock_builder.Object.UseActiveSessions(filter);
            //Assess
            ActiveSessionMiddleware.MiddlewareParam middleware_param = ts.VerifyAndGetMidddewareParam();
            IMiddlewareFilter middleware_filter = middleware_param.Filters[0].Create(0);
            Assert.False(middleware_param.AcceptAll);
            Assert.Single(middleware_param.Filters);
            Assert.True(middleware_filter.Apply(fake_context.Object).WasMapped); //REQ_PATH1
            Assert.False(middleware_filter.Apply(fake_context.Object).WasMapped); //REQ_PATH2
        }

        //Test case: two UseActiveSession calls with and without delegate
        [Fact]
        public void UseActiveSessions_TwiceWithAndWithoutDelegate()
        {
            //Arrange
            TestSetup ts = new TestSetup();
            Func<HttpContext, Boolean> filter = context => context.Request.Path.Equals("/");
            //Act
            ts.mock_builder.Object.UseActiveSessions(filter);
            ts.mock_builder.Object.UseActiveSessions();
            //Assess
            ts.mock_builder.Verify(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
            ActiveSessionMiddleware.MiddlewareParam middleware_param = ts.VerifyAndGetMidddewareParam();
            Assert.True(middleware_param.AcceptAll);
            Assert.Single(middleware_param.Filters);
            Assert.True(ReferenceEquals(filter, (middleware_param.Filters[0] as SimplePredicateFilterSource)?.Predicate));
        }

        //Test case: two UseActiveSession calls with two different delegates
        [Fact]
        public void UseActiveSessions_TwiceWithTwoDelegates()
        {
            //Arrange
            TestSetup ts = new TestSetup();
            Func<HttpContext, Boolean> filter1 = context => context.Request.Path.Equals("/");
            Func<HttpContext, Boolean> filter2 = context => context.Request.Path.Equals("/test");
            //Act
            ts.mock_builder.Object.UseActiveSessions(filter1);
            ts.mock_builder.Object.UseActiveSessions(filter2);
            //Assess
            ts.mock_builder.Verify(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
            ActiveSessionMiddleware.MiddlewareParam middleware_param = ts.VerifyAndGetMidddewareParam();
            Assert.False(middleware_param.AcceptAll);
            Assert.Equal(2,middleware_param.Filters.Count);
            Assert.True(ReferenceEquals(filter1, (middleware_param.Filters[0] as SimplePredicateFilterSource)?.Predicate));
            Assert.True(ReferenceEquals(filter2, (middleware_param.Filters[1] as SimplePredicateFilterSource)?.Predicate));
        }

    }
}
