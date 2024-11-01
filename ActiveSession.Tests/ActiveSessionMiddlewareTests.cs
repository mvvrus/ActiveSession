using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ActiveSessionMiddlewareTests
    {
        //Test case: Create ActiveSessionMiddleware
        [Fact]
        public void ConstructActiveSessionMiddleware()
        {
            //Arrange
            MiddlewareCreateTestSetup test_setup = new MiddlewareCreateTestSetup(new ActiveSessionOptions());
            //Act
            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                new ActiveSessionMiddleware.MiddlewareParam{AcceptAll=true},
                test_setup.StubStore.Object,
                test_setup.LoggerFactory,
                test_setup.StubOptions.Object
            );
            //Assess
            Assert.Equal(test_setup.MockNextDelegate.Object, middleware.Next);
            Assert.Equal(test_setup.StubStore.Object, middleware.Store);
        }

        //Test case: Invoke ActiveSessionMiddleware normally
        [Fact]
        public void InvokeActiveSessionMiddleware_Normal()
        {
            //Arrange
            NextDelegateHost spy_host = new NextDelegateHost();
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(
                new ActiveSessionOptions(), spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                new ActiveSessionMiddleware.MiddlewareParam { AcceptAll=true },
                test_setup.StubStore.Object,
                test_setup.LoggerFactory,
                test_setup.StubOptions.Object
            );
            //Act
            middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();
            //Assess
            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Once);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(REQUEST_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Once);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.StubStore.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        //Test case: Invoke CactiveSessionMiddleware substituting RequestServices by IActiveSession.SessionServices 
        [Fact]
        public void InvokeActiveSessionMiddleware_UseSessionServices()
        {
            //Arrange
            NextDelegateHost spy_host = new NextDelegateHost();
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(
                new ActiveSessionOptions { UseSessionServicesAsRequestServices=true, PreloadActiveSession=false}, spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                new ActiveSessionMiddleware.MiddlewareParam { AcceptAll=true },
                test_setup.StubStore.Object,
                test_setup.LoggerFactory,
                test_setup.StubOptions.Object
            );
            //Act
            middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();
            //Assess
            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Once);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(SESSION_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Once);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.StubStore.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        //Test ActiveSession no pre-loading
        [Fact]
        public void InvokeActiveSessionMiddleware_NoPreload()
        {
            //Arrange
            NextDelegateHost spy_host = new NextDelegateHost();
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(
                new ActiveSessionOptions { PreloadActiveSession=false }, spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                new ActiveSessionMiddleware.MiddlewareParam { AcceptAll=true },
                test_setup.StubStore.Object,
                test_setup.LoggerFactory,
                test_setup.StubOptions.Object
            );
            //Act
            middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();
            //Assess
            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Never);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(REQUEST_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Once);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.StubStore.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        //Test case: Invoke CactiveSessionMiddleware causing exception
        [Fact]
        public void InvokeActiveSessionMiddleware_WithException()
        {
            //Arrange
            NextDelegateHost spy_host = new NextDelegateHost(new TestException());
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(new ActiveSessionOptions(), spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                new ActiveSessionMiddleware.MiddlewareParam { AcceptAll=true },
                test_setup.StubStore.Object,
                test_setup.LoggerFactory,
                test_setup.StubOptions.Object
            );
            Boolean test_throws = false;
            //Act
            try {
                middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();
            }
            catch(TestException) {
                test_throws=true;
            }
            //Assess
            Assert.True(test_throws,"Test exception was not thrown");
            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Once);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(REQUEST_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Never);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.StubStore.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        //Test case: Invoke ActiveSessionMiddleware with a filtered out request
        [Fact]
        public void InvokeActiveSessionMiddleware_FilteredOut()
        {
            //Arrange
            NextDelegateHost spy_host = new NextDelegateHost();
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(
                new ActiveSessionOptions(), spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            HttpContext? filtered_context = null;
            Func<HttpContext, Boolean> neg_filter =  context => { filtered_context=context; return false; };
            ActiveSessionMiddleware.MiddlewareParam mwparam=new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add((SimplePredicateFilterSource)neg_filter);
            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                mwparam,
                test_setup.StubStore.Object,
                test_setup.LoggerFactory,
                test_setup.StubOptions.Object
            );
            //Act
            middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();
            //Assess
            Assert.True(ReferenceEquals(test_context.MockContext.Object, filtered_context));
            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Never);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(REQUEST_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Null(spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Never);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.StubStore.Verify(test_setup.ClearCallExpression, Times.Never);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        //Test case: Invoke ActiveSessionMiddleware with a filtered out request be made and AcceptAll is set to true
        [Fact]
        public void InvokeActiveSessionMiddleware_FilteredOutAcceptAll()
        {
            //Arrange
            NextDelegateHost spy_host = new NextDelegateHost();
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(
                new ActiveSessionOptions(), spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            HttpContext? filtered_context = null;
            Func<HttpContext, Boolean> neg_filter = context => { filtered_context=context; return false; };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add((SimplePredicateFilterSource)neg_filter);
            mwparam.AcceptAll=true;
            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                mwparam,
                test_setup.StubStore.Object,
                test_setup.LoggerFactory,
                test_setup.StubOptions.Object
            );
            //Act
            middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();
            //Assess
            Assert.Null(filtered_context);
            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Once);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(REQUEST_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Once);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.StubStore.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        //Test case: Invoke ActiveSessionMiddleware built with two filters with a request filtered out the second of them
        [Fact]
        public void InvokeActiveSessionMiddleware_TwoFilters()
        {
            const string PATH1 = "/path1";
            const string PATH2 = "/path2";
            //Arrange
            NextDelegateHost spy_host = new NextDelegateHost();
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(
                new ActiveSessionOptions(), spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            test_context.MockContext.SetupGet(s => s.Request.Path).Returns(PATH2+"/rest");
            HttpContext? filtered_context1 = null;
            Func<HttpContext, Boolean> filter1 = context => { filtered_context1=context; return context.Request.Path.StartsWithSegments(PATH1); };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add((SimplePredicateFilterSource)filter1);
            HttpContext? filtered_context2 = null;
            Func<HttpContext, Boolean> filter2 = context => { filtered_context2=context; return context.Request.Path.StartsWithSegments(PATH2); };
            mwparam.Filters.Add((SimplePredicateFilterSource)filter2);
            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                mwparam,
                test_setup.StubStore.Object,
                test_setup.LoggerFactory,
                test_setup.StubOptions.Object
            );
            //Act
            middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();
            //Assess
            Assert.True(ReferenceEquals(test_context.MockContext.Object, filtered_context1));
            Assert.True(ReferenceEquals(test_context.MockContext.Object, filtered_context2));
            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Once);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(REQUEST_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Once);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.StubStore.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }



        class TestException :Exception
        {
            public TestException() : base() { }
        }

        class MiddlewareCreateTestSetup
        {
            public Mock<RequestDelegate> MockNextDelegate { get; init; }
            public Mock<IActiveSessionStore> StubStore { get; init; }
            public Mock<IOptions<ActiveSessionOptions>> StubOptions { get; init; }
            public ILoggerFactory LoggerFactory { get=>_loggerFactory.LoggerFactory;}
            readonly MockedLoggerFactory _loggerFactory;
            ActiveSessionOptions _options;

            public MiddlewareCreateTestSetup(ActiveSessionOptions Options)
            {
                MockNextDelegate=new Mock<RequestDelegate>();
                StubStore=new Mock<IActiveSessionStore>();
                _options=Options;
                StubOptions=new Mock<IOptions<ActiveSessionOptions>>();
                StubOptions.SetupGet(x => x.Value).Returns(_options);
                _loggerFactory=new MockedLoggerFactory();
                _loggerFactory.MonitorLoggerCategory(ActiveSessionConstants.LOGGING_CATEGORY_NAME);

            }
        }

        class MiddlewareInvokeTestSetup : MiddlewareCreateTestSetup
        {
            public Mock<IActiveSession> FakeActiveSession { get; init; }
            public Mock<IActiveSessionFeature> MockFeature { get; init; }

            public Mock<ISession> StubSession { get; init; }
            public readonly Expression<Func<RequestDelegate,Task>> NextCallExpression= 
                x => x.Invoke(It.IsAny<HttpContext>());
            public readonly Expression<Func<IActiveSessionFeature, Task>> LoadAsyncCallExpression = 
                s => s.LoadAsync(It.IsAny<CancellationToken>());
            public readonly Expression<Func<IActiveSessionFeature, Task>> CommitAsyncCallExpression =
                s => s.CommitAsync(It.IsAny<CancellationToken>());
            public readonly Expression<Action<IActiveSessionStore>> ClearCallExpression = 
                s => s.ReleaseFeatureObject(It.IsAny<IActiveSessionFeature>());

            Mock<IServiceProvider> _stubSessionServices { get; init; }
            RequestDelegate? _spyDelegate;


            public MiddlewareInvokeTestSetup(ActiveSessionOptions Options, RequestDelegate? SpyDelegate=null)
                : base(Options)
            {
                _spyDelegate=SpyDelegate;
                _stubSessionServices=new Mock<IServiceProvider>();
                _stubSessionServices.Setup(s => s.GetService(typeof(ServiceProviderIdent)))
                    .Returns(new ServiceProviderIdent(SESSION_SERVICES_IDENT));

                FakeActiveSession=new Mock<IActiveSession>();
                FakeActiveSession.SetupGet(s => s.IsAvailable).Returns(true);
                FakeActiveSession.SetupGet(s => s.SessionServices).Returns(_stubSessionServices.Object);

                MockFeature=new Mock<IActiveSessionFeature>();
                MockFeature.Setup(LoadAsyncCallExpression).Returns(Task.CompletedTask);
                MockFeature.Setup(s => s.IsLoaded).Returns(true);
                MockFeature.SetupGet(s => s.ActiveSession).Returns(FakeActiveSession.Object);
                MockFeature.Setup(CommitAsyncCallExpression).Returns(Task.CompletedTask);
                StubStore.Setup(ClearCallExpression);

                StubSession=new Mock<ISession>();
                StubSession.SetupGet(x => x.Id).Returns(FAKE_SESSION_ID);
                StubSession.SetupGet(x => x.IsAvailable).Returns(true);

                StubStore.Setup(x => x.AcquireFeatureObject(It.IsAny<ISession>(), It.IsAny<String>(), It.IsAny<String ?>()));
                StubStore.Setup(x => x.AcquireFeatureObject(StubSession.Object, It.IsAny<String>(), null)).Returns(MockFeature.Object);

                MockNextDelegate.Setup(NextCallExpression).Returns((HttpContext s) => _spyDelegate?.Invoke(s)??Task.CompletedTask);
            }
        }

        const string FAKE_SESSION_ID = "FAKE_SESSION_ID";
        const string FAKE_TRACE_ID = "FAKE_TRACE_ID";
        const string REQUEST_SERVICES_IDENT = "RequestServces";
        const string SESSION_SERVICES_IDENT = "SessionServces";

        class ServiceProviderIdent
        {
            public String Ident { get; init; }
            public ServiceProviderIdent(String Ident)
            {
                this.Ident=Ident;
            }
        }

        class FakeHttpContext
        {
            public Mock<HttpContext> MockContext { get; init; }
            public IActiveSessionFeature? ShadowActiveSessionFeature { get { return _shadowActiveSessionFeature; } }
            public Mock<IServiceProvider> StubRequestServices { get; init; }
            Mock<IFeatureCollection> _fakeFeatureCollection { get; init; }
            IActiveSessionFeature? _shadowActiveSessionFeature;

            public FakeHttpContext(ISession Session)
            {
                _fakeFeatureCollection=new Mock<IFeatureCollection>();
                _fakeFeatureCollection.Setup(x => x.Get<IActiveSessionFeature>()).Returns(()=> _shadowActiveSessionFeature);
                _fakeFeatureCollection.Setup(x => x.Set(It.IsAny<IActiveSessionFeature>()))
                    .Callback((IActiveSessionFeature s) => { _shadowActiveSessionFeature=s; });

                StubRequestServices=new Mock<IServiceProvider>();
                StubRequestServices.Setup(s => s.GetService(typeof(ServiceProviderIdent)))
                    .Returns(new ServiceProviderIdent(REQUEST_SERVICES_IDENT));

                MockContext=new Mock<HttpContext>();
                MockContext.SetupProperty(x => x.RequestServices, StubRequestServices.Object);
                MockContext.SetupGet(x => x.TraceIdentifier).Returns(FAKE_TRACE_ID);
                MockContext.SetupGet(x => x.Features).Returns(_fakeFeatureCollection.Object);
                MockContext.SetupGet(x => x.Session).Returns(Session);

            }
        }


        class NextDelegateHost
        {
            Exception? _exceptionToThrow;
            public IActiveSessionFeature? Feature;
            public String RequestServicesId;

            public static String UNNKNOW_REQUEST_SERICES = "unkown";

            public NextDelegateHost(Exception? ExceptionToThrow=null)
            {
                _exceptionToThrow=ExceptionToThrow;
                RequestServicesId=UNNKNOW_REQUEST_SERICES;
            }

            public Task SpyDelegate(HttpContext Context)
            {
                RequestServicesId=(Context.RequestServices.GetService<ServiceProviderIdent>()?.Ident)
                    ??UNNKNOW_REQUEST_SERICES;
                Feature=Context.Features.Get<IActiveSessionFeature>();

                return _exceptionToThrow==null ? Task.CompletedTask : Task.FromException(_exceptionToThrow);
            }
        }
    }
}
