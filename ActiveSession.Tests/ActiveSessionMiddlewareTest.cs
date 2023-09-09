using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ActiveSessionMiddlewareTest
    {
        class MiddlewareCreateTestSetup
        {
            public Mock<RequestDelegate> MockNext { get; init; }
            public Mock<IActiveSessionStore> StubStore { get; init; }
            public Mock<IOptions<ActiveSessionOptions>> FakeOptions { get; init; }
            ActiveSessionOptions _options;

            public MiddlewareCreateTestSetup(Boolean UseSessionServicesAsRequestServices)
            {
                MockNext=new Mock<RequestDelegate>();
                StubStore=new Mock<IActiveSessionStore>();
                _options=new ActiveSessionOptions();
                _options.UseSessionServicesAsRequestServices=UseSessionServicesAsRequestServices;
                FakeOptions=new Mock<IOptions<ActiveSessionOptions>>();
                FakeOptions.SetupGet(x => x.Value).Returns(_options);
            }
        }

        class MiddlewareInvokeTestSetup : MiddlewareCreateTestSetup
        {
            public Mock<MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession> FakeActiveSession { get; init; }
            public Mock<IServiceProvider> FakeSessionServices { get; init; }
            public Mock<ActiveSessionFeature> FakeFeature;
            public Mock<ISession> FakeSession { get; init; }
            RequestDelegate? _spyDelegate;

            public MiddlewareInvokeTestSetup(Boolean UseSessionServicesAsRequestServices, RequestDelegate? SpyDelegate=null)
                : base(UseSessionServicesAsRequestServices)
            {
                _spyDelegate=SpyDelegate;
                FakeSessionServices=new Mock<IServiceProvider>();
                FakeSessionServices.Setup(s => s.GetService(typeof(ServiceProviderIdent))).Returns(new ServiceProviderIdent(SESSION_SERVICES_IDENT));

                FakeActiveSession=new Mock<MVVrus.AspNetCore.ActiveSession.Internal.ActiveSession>();
                FakeActiveSession.SetupGet(s => s.IsAvailable).Returns(true);
                FakeActiveSession.SetupGet(s => s.SessionServices).Returns(FakeSessionServices.Object);

                FakeFeature=new Mock<ActiveSessionFeature>();
                FakeFeature.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
                FakeFeature.Setup(s => s.IsLoaded).Returns(true);
                FakeFeature.SetupGet(s => s.ActiveSession).Returns(FakeActiveSession.Object);
                FakeFeature.Setup(s => s.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
                FakeFeature.Setup(s => s.Clear());

                FakeSession=new Mock<ISession>();
                FakeSession.SetupGet(x => x.Id).Returns(FAKE_SESSION_ID);
                FakeSession.SetupGet(x => x.IsAvailable).Returns(true);

                StubStore.Setup(x => x.CreateFeatureObject(It.IsAny<ISession>(), It.IsAny<String>()));
                StubStore.Setup(x => x.CreateFeatureObject(FakeSession.Object, It.IsAny<String>())).Returns(FakeFeature.Object);

                MockNext.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                    .Returns((HttpContext s) => _spyDelegate?.Invoke(s)??Task.CompletedTask);
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
            public Mock<HttpContext> FakeContext { get; init; }
            Mock<IFeatureCollection> StubFeatureCollection { get; init; }
            Mock<IServiceProvider> FakeRequestServices { get; init; }
            IActiveSessionFeature? _shadowAcrtveSessionFeature;

            public FakeHttpContext(ISession Session)
            {
                StubFeatureCollection=new Mock<IFeatureCollection>();
                StubFeatureCollection.Setup(x => x.Get<IActiveSessionFeature>()).Returns(_shadowAcrtveSessionFeature);
                StubFeatureCollection.Setup(x => x.Set<IActiveSessionFeature>(It.IsAny<IActiveSessionFeature>()))
                    .Callback((IActiveSessionFeature s) => { _shadowAcrtveSessionFeature=s; });

                FakeRequestServices=new Mock<IServiceProvider>();
                FakeRequestServices.Setup(s => s.GetService(typeof(ServiceProviderIdent))).Returns(new ServiceProviderIdent(REQUEST_SERVICES_IDENT));

                FakeContext=new Mock<HttpContext>();
                FakeContext.SetupProperty(x => x.RequestServices, FakeRequestServices.Object);
                FakeContext.SetupGet(x => x.TraceIdentifier).Returns(FAKE_TRACE_ID);
                FakeContext.SetupGet(x => x.Features).Returns(StubFeatureCollection.Object);
                FakeContext.SetupGet(x => x.Session).Returns(Session);

            }
        }


        [Fact]
        public void ConstructActiveSessionMiddleware()
        {
            MiddlewareCreateTestSetup test_setup = new MiddlewareCreateTestSetup(true);

            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(test_setup.MockNext.Object, test_setup.StubStore.Object, null, test_setup.FakeOptions.Object);

            Assert.Equal(test_setup.MockNext.Object, middleware.Next);
            Assert.Equal(test_setup.StubStore.Object, middleware.Store);
            Assert.Equal(test_setup.FakeOptions.Object.Value.UseSessionServicesAsRequestServices, middleware.useSessionServicesAsRequestServices);
        }

        [Fact]
        public void InvokeActiveSessionMiddleware()
        {
            MiddlewareInvokeTestSetup test_setup = new MiddlewareInvokeTestSetup(true);
            FakeHttpContext test_context = new FakeHttpContext(test_setup.FakeSession.Object);

            ActiveSessionMiddleware middleware = new ActiveSessionMiddleware(test_setup.MockNext.Object, test_setup.StubStore.Object, null, test_setup.FakeOptions.Object);
            middleware.Invoke(test_context.FakeContext.Object).GetAwaiter().GetResult();
            
        }

    }
}
