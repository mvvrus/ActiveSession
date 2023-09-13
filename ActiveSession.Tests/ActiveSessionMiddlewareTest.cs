using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
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
    public class ActiveSessionMiddlewareTest
    {
        [Fact]
        public void ConstructActiveSessionMiddleware()
        {
            MiddlewareCreateTestSetup test_setup = new MiddlewareCreateTestSetup(true);

            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                test_setup.StubStore.Object,
                null,
                test_setup.StubOptions.Object
            );

            Assert.Equal(test_setup.MockNextDelegate.Object, middleware.Next);
            Assert.Equal(test_setup.StubStore.Object, middleware.Store);
            Assert.Equal(test_setup.StubOptions.Object.Value.UseSessionServicesAsRequestServices, middleware.UseSessionServicesAsRequestServices);
        }

        [Fact]
        public void InvokeActiveSessionMiddleware_Normal()
        {
            NextDelegateHost spy_host = new NextDelegateHost();
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(false, spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);

            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                test_setup.StubStore.Object,
                null,
                test_setup.StubOptions.Object
            );
            middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();

            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Never);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(REQUEST_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Once);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.MockFeatureControl.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        [Fact]
        public void InvokeActiveSessionMiddleware_UseSessionServices()
        {
            NextDelegateHost spy_host = new NextDelegateHost();
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(true, spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);

            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                test_setup.StubStore.Object,
                null,
                test_setup.StubOptions.Object
            );
            middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();

            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Once);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(SESSION_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Once);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.MockFeatureControl.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        [Fact]
        public void InvokeActiveSessionMiddleware_WithException()
        {
            NextDelegateHost spy_host = new NextDelegateHost(new TestException());
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(false, spy_host.SpyDelegate);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.StubSession.Object);

            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(
                test_setup.MockNextDelegate.Object,
                test_setup.StubStore.Object,
                null,
                test_setup.StubOptions.Object
            );
            Boolean test_throws = false;
            try {
                middleware.Invoke(test_context.MockContext.Object).GetAwaiter().GetResult();
            }
            catch(TestException) {
                test_throws=true;
            }

            Assert.True(test_throws,"Test exception was not thrown");
            test_setup.MockFeature.Verify(test_setup.LoadAsyncCallExpression, Times.Never);
            test_setup.MockNextDelegate.Verify(test_setup.NextCallExpression, Times.Once);
            Assert.Equal(REQUEST_SERVICES_IDENT, spy_host.RequestServicesId);
            Assert.Equal(test_setup.MockFeature.Object, spy_host.Feature);
            test_setup.MockFeature.Verify(test_setup.CommitAsyncCallExpression, Times.Never);
            Assert.Null(test_context.ShadowActiveSessionFeature);
            test_setup.MockFeatureControl.Verify(test_setup.ClearCallExpression, Times.Once);
            Assert.Equal(test_context.StubRequestServices.Object, test_context.MockContext.Object.RequestServices);
        }

        class TestException:Exception
        {
            public TestException() : base() { }
        }

        class MiddlewareCreateTestSetup
        {
            public Mock<RequestDelegate> MockNextDelegate { get; init; }
            public Mock<IActiveSessionStore> StubStore { get; init; }
            public Mock<IOptions<ActiveSessionOptions>> StubOptions { get; init; }
            ActiveSessionOptions _options;

            public MiddlewareCreateTestSetup(Boolean UseSessionServicesAsRequestServices)
            {
                MockNextDelegate=new Mock<RequestDelegate>();
                StubStore=new Mock<IActiveSessionStore>();
                _options=new ActiveSessionOptions();
                _options.UseSessionServicesAsRequestServices=UseSessionServicesAsRequestServices;
                StubOptions=new Mock<IOptions<ActiveSessionOptions>>();
                StubOptions.SetupGet(x => x.Value).Returns(_options);
            }
        }

        class MiddlewareInvokeTestSetup : MiddlewareCreateTestSetup
        {
            public Mock<IActiveSession> FakeActiveSession { get; init; }
            public Mock<IActiveSessionFeature> MockFeature { get; init; }
            public Mock<IActiveSessionFeatureControl> MockFeatureControl;

            public Mock<ISession> StubSession { get; init; }
            public readonly Expression<Func<RequestDelegate,Task>> NextCallExpression= 
                x => x.Invoke(It.IsAny<HttpContext>());
            public readonly Expression<Func<IActiveSessionFeature, Task>> LoadAsyncCallExpression = 
                s => s.LoadAsync(It.IsAny<CancellationToken>());
            public readonly Expression<Func<IActiveSessionFeature, Task>> CommitAsyncCallExpression =
                s => s.CommitAsync(It.IsAny<CancellationToken>());
            public readonly Expression<Action<IActiveSessionFeatureControl>> ClearCallExpression = s => s.Clear();

            Mock<IServiceProvider> _stubSessionServices { get; init; }
            RequestDelegate? _spyDelegate;


            public MiddlewareInvokeTestSetup(Boolean UseSessionServicesAsRequestServices, RequestDelegate? SpyDelegate=null)
                : base(UseSessionServicesAsRequestServices)
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
                MockFeatureControl=MockFeature.As<IActiveSessionFeatureControl>();
                MockFeatureControl.Setup(ClearCallExpression);

                StubSession=new Mock<ISession>();
                StubSession.SetupGet(x => x.Id).Returns(FAKE_SESSION_ID);
                StubSession.SetupGet(x => x.IsAvailable).Returns(true);

                StubStore.Setup(x => x.CreateFeatureObject(It.IsAny<ISession>(), It.IsAny<String>()));
                StubStore.Setup(x => x.CreateFeatureObject(StubSession.Object, It.IsAny<String>())).Returns(MockFeature.Object);

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
