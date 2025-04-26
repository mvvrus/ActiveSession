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
                new ActiveSessionMiddleware.MiddlewareParam { AcceptAll=true },
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
                new ActiveSessionOptions { UseSessionServicesAsRequestServices=true, PreloadActiveSession=false }, spy_host.SpyDelegate);
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
            Assert.True(test_throws, "Test exception was not thrown");
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
            MiddlewareFilterTestSetup test_setup = new MiddlewareFilterTestSetup();
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            Func<HttpContext, Boolean> neg_filter = context => { return false; };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add((SimplePredicateFilterSource)neg_filter);
            test_setup.MakeMiddleware(mwparam);
            //Act
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.False(test_setup.ActiveSessionWasAvailable);
        }

        //Test case: Invoke ActiveSessionMiddleware with a filtered out request be made and AcceptAll is set to true
        [Fact]
        public void InvokeActiveSessionMiddleware_FilteredOutAcceptAll()
        {
            //Arrange
            MiddlewareFilterTestSetup test_setup = new MiddlewareFilterTestSetup();
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            Func<HttpContext, Boolean> neg_filter = context => { return false; };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add((SimplePredicateFilterSource)neg_filter);
            mwparam.AcceptAll=true;
            test_setup.MakeMiddleware(mwparam);
            //Act
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
        }

        const string PATH1 = "/path1";
        const string PATH2 = "/path2";
        //Test case: Invoke ActiveSessionMiddleware built with two filters with a request filtered out the second of them
        [Fact]
        public void InvokeActiveSessionMiddleware_TwoFilters()
        {
            //Arrange
            MiddlewareFilterTestSetup test_setup = new MiddlewareFilterTestSetup();
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            Func<HttpContext, Boolean> filter1 = context => { return context.Request.Path.StartsWithSegments(PATH1); };
            Func<HttpContext, Boolean> filter2 = context => { return context.Request.Path.StartsWithSegments(PATH2); };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add((SimplePredicateFilterSource)filter1);
            mwparam.Filters.Add((SimplePredicateFilterSource)filter2);
            test_setup.MakeMiddleware(mwparam);
            //Act
            test_context.SetPath(PATH1+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
            //Act
            test_context.SetPath(PATH2+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
            //Act
            test_context.SetPath("/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.False(test_setup.ActiveSessionWasAvailable);
        }

        const String SUFFIX1 = "1";
        const String SUFFIX2 = "2";

        //Test group: set IActiveSession.Id suffix - single filter w/o AcceptAll
        [Fact]
        public void Invoke_SuffixSinglleFilterNotAcceptAll()
        {
            //Arrange
            MiddlewareFilterTestSetup test_setup = new MiddlewareFilterTestSetup();
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            Func<HttpContext, Boolean> filter1 = context => { return context.Request.Path.StartsWithSegments(PATH1); };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add(new PredicateWithSuffixFilterSource(filter1, SUFFIX1));
            test_setup.MakeMiddleware(mwparam);
            //Act
            test_context.SetPath(PATH1+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Equal(SUFFIX1, test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
            //Act
            test_context.SetPath(PATH2+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.False(test_setup.ActiveSessionWasAvailable);
        }

        //Test group: set IActiveSession.Id suffix - single filter with AcceptAll
        [Fact]
        public void Invoke_SuffixSinglleFilterAcceptAll()
        {
            //Arrange
            MiddlewareFilterTestSetup test_setup = new MiddlewareFilterTestSetup();
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            Func<HttpContext, Boolean> filter1 = context => { return context.Request.Path.StartsWithSegments(PATH1); };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add(new PredicateWithSuffixFilterSource(filter1, SUFFIX1));
            mwparam.AcceptAll=true;
            test_setup.MakeMiddleware(mwparam);
            //Act
            test_context.SetPath(PATH1+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Equal(SUFFIX1, test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
            //Act
            test_context.SetPath(PATH2+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
        }

        //Test group: set IActiveSession.Id suffix - two filters w/o AcceptAll
        [Fact]
        public void Invoke_SuffixTwoFilters()
        {
            //Arrange
            MiddlewareFilterTestSetup test_setup = new MiddlewareFilterTestSetup();
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            Func<HttpContext, Boolean> filter1 = context => { return context.Request.Path.StartsWithSegments(PATH1); };
            Func<HttpContext, Boolean> filter2 = context => { return context.Request.Path.StartsWithSegments(PATH2); };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add(new PredicateWithSuffixFilterSource(filter1, SUFFIX1));
            mwparam.Filters.Add(new PredicateWithSuffixFilterSource(filter2, SUFFIX2));
            test_setup.MakeMiddleware(mwparam);
            //Act
            test_context.SetPath(PATH1+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Equal(SUFFIX1, test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
            //Act
            test_context.SetPath(PATH2+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Equal(SUFFIX2, test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
            //Act
            test_context.SetPath("/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.False(test_setup.ActiveSessionWasAvailable);
        }

        //Test group: set IActiveSession.Id suffix - two filters w/o AcceptAll,
        //  1st - does not set suffix, 2nd - accepts only a subset of paths for the first.
        [Fact]
        public void Invoke_TwoFiltersOneSuffix()
        {
            const String SUBPATH1 = "/sub1";
            //Arrange
            MiddlewareFilterTestSetup test_setup = new MiddlewareFilterTestSetup();
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);
            Func<HttpContext, Boolean> filter1 = context => { return context.Request.Path.StartsWithSegments(PATH1); };
            Func<HttpContext, Boolean> filter2 = context => { return context.Request.Path.StartsWithSegments(PATH1+SUBPATH1); };
            ActiveSessionMiddleware.MiddlewareParam mwparam = new ActiveSessionMiddleware.MiddlewareParam();
            mwparam.Filters.Add(new SimplePredicateFilterSource(filter1));
            mwparam.Filters.Add(new PredicateWithSuffixFilterSource(filter2, SUFFIX2));
            test_setup.MakeMiddleware(mwparam);
            //Act
            test_context.SetPath(PATH1+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
            //Act
            test_context.SetPath(PATH1+SUBPATH1+"/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Equal(SUFFIX2, test_setup.ActiveSessionSuffix);
            Assert.True(test_setup.ActiveSessionWasAvailable);
            //Act
            test_context.SetPath("/rest");
            test_setup.Invoke(test_context.MockContext.Object);
            //Assess
            Assert.True(test_setup.NextRequestDelegateInvoked);
            Assert.Null(test_setup.ActiveSessionSuffix);
            Assert.False(test_setup.ActiveSessionWasAvailable);
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
            public Mock<IActiveSessionFeatureImpl> MockFeature { get; init; }

            public Mock<ISession> StubSession { get; init; }
            public readonly Expression<Func<RequestDelegate,Task>> NextCallExpression= 
                x => x.Invoke(It.IsAny<HttpContext>());
            public readonly Expression<Func<IActiveSessionFeatureImpl, Task>> LoadAsyncCallExpression = 
                s => s.LoadAsync(It.IsAny<CancellationToken>());
            public readonly Expression<Func<IActiveSessionFeatureImpl, Task>> CommitAsyncCallExpression =
                s => s.CommitAsync(It.IsAny<CancellationToken>());
            public readonly Expression<Action<IActiveSessionStore>> ClearCallExpression = 
                s => s.ReleaseFeatureObject(It.IsAny<IActiveSessionFeatureImpl>());

            Mock<IServiceProvider> _stubSessionServices { get; init; }
            protected RequestDelegate? _spyDelegate;

            String? _suffix = null;
            protected String? Suffix { get => _suffix; }
            public void ResetSuffix() { _suffix=null; }
            void FetchCallback(ISession ignore1, String? ignore2, String? s, IServiceProvider sp) { _suffix=s; }


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

                MockFeature=new Mock<IActiveSessionFeatureImpl>();
                MockFeature.Setup(LoadAsyncCallExpression).Returns(Task.CompletedTask);
                MockFeature.Setup(s => s.IsLoaded).Returns(true);
                MockFeature.SetupGet(s => s.ActiveSession).Returns(FakeActiveSession.Object);
                MockFeature.Setup(CommitAsyncCallExpression).Returns(Task.CompletedTask);
                StubStore.Setup(ClearCallExpression);

                StubSession=new Mock<ISession>();
                StubSession.SetupGet(x => x.Id).Returns(FAKE_SESSION_ID);
                StubSession.SetupGet(x => x.IsAvailable).Returns(true);

                StubStore.Setup(x => x.AcquireFeatureObject(It.IsAny<ISession>(), It.IsAny<String?>(), It.IsAny<String ?>(), It.IsAny<IServiceProvider>()));
                StubStore.Setup(x => x.AcquireFeatureObject(StubSession.Object, It.IsAny<String?>(), It.IsAny<String?>(), It.IsAny<IServiceProvider>()))
                    .Callback(FetchCallback)
                    .Returns(MockFeature.Object);

                MockNextDelegate.Setup(NextCallExpression).Returns((HttpContext s) => _spyDelegate?.Invoke(s)??Task.CompletedTask);
            }
        }

        class MiddlewareFilterTestSetup : MiddlewareInvokeTestSetup
        {

            ActiveSessionMiddleware _middleware = null!;
            public Boolean ActiveSessionWasAvailable { get; private set; }
            public String? ActiveSessionSuffix { get; private set; }
            public Boolean NextRequestDelegateInvoked { get; private set; }

            public MiddlewareFilterTestSetup() :
                base(new ActiveSessionOptions())
            {
                _spyDelegate=SpyDelegate;
            }

            public void MakeMiddleware(ActiveSessionMiddleware.MiddlewareParam Param)
            {
                _middleware = new ActiveSessionMiddleware(
                    MockNextDelegate.Object,
                    Param,
                    StubStore.Object,
                    LoggerFactory,
                    StubOptions.Object
                );
            }

            public void Invoke(HttpContext Context)
            {
                ActiveSessionWasAvailable = false;
                ActiveSessionSuffix = null;
                ResetSuffix();
                NextRequestDelegateInvoked = false;
                _middleware!. Invoke(Context).GetAwaiter().GetResult();
            }

            private Task SpyDelegate(HttpContext Context)
            {
                ActiveSessionWasAvailable = Context.GetActiveSession().IsAvailable;
                ActiveSessionSuffix = ActiveSessionWasAvailable?Suffix:null;
                NextRequestDelegateInvoked =true;
                return Task.CompletedTask;
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
            public IActiveSessionFeature? ShadowActiveSessionFeature { get { return _featureCollection[typeof(IActiveSessionFeature)] as IActiveSessionFeature; } }
            public Mock<IServiceProvider> StubRequestServices { get; init; }
            readonly IFeatureCollection _featureCollection;
            String? _path = null;

            public FakeHttpContext(ISession Session)
            {
                StubRequestServices=new Mock<IServiceProvider>();
                StubRequestServices.Setup(s => s.GetService(typeof(ServiceProviderIdent)))
                    .Returns(new ServiceProviderIdent(REQUEST_SERVICES_IDENT));
                _featureCollection=new FeatureCollection();
                MockContext =new Mock<HttpContext>();
                MockContext.SetupProperty(x => x.RequestServices, StubRequestServices.Object);
                MockContext.SetupGet(x => x.TraceIdentifier).Returns(FAKE_TRACE_ID);
                MockContext.SetupGet(x => x.Features).Returns(_featureCollection);
                MockContext.SetupGet(x => x.Session).Returns(Session);
                MockContext.SetupGet(s => s.Request.Path).Returns(()=>_path);
            }

            public void SetPath(String Path)
            {
                _path=Path;
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
