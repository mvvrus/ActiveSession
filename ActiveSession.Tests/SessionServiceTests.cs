using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class SessionServiceTests
    {
        public interface IDummyService 
        {
            String Property { get; }
        };

        //Test group: ActiveSessionService class
        [Fact]
        public void ActiveSessionService()
        {
            //Arrange
            Boolean from_session = true;
            Mock<IDummyService> dummy_service = new Mock<IDummyService>();
            Mock<IServiceProvider> stub_sp= new Mock<IServiceProvider>();
            stub_sp.Setup(s => s.GetService(typeof(IDummyService))).Returns(dummy_service.Object);
            Mock<ActiveSessionRef> stub_sp_ref=new Mock<ActiveSessionRef>();
            stub_sp_ref.SetupGet(s=>s.IsFromSession).Returns(()=>from_session);
            stub_sp_ref.SetupGet(s => s.Services).Returns(stub_sp.Object);
            ISessionService<IDummyService> r1;
            ISessionService<ILoggerFactory> r2;
            //Test case: existing service from session
            //Act
            r1=new SessionService<IDummyService>(stub_sp_ref.Object);
            //Assess
            Assert.True(ReferenceEquals(dummy_service.Object,r1.Service));
            Assert.True(r1.IsFromSession);

            //Test case: non-existing service
            //Act
            r2=new SessionService<ILoggerFactory>(stub_sp_ref.Object);
            //Assess
            Assert.Null(r2.Service);

            //Test case: existing service from request context
            //Arrange more
            from_session=false;
            //Act
            r1=new SessionService<IDummyService>(stub_sp_ref.Object);
            //Assess
            Assert.False(r1.IsFromSession);
        }

        //Test group: SessionServiceProviderRef
        [Fact]
        public void SessionServiceProviderRef()
        {
            //Arrange
            Boolean avail = true;
            Mock<IServiceProvider> dummy_req_sp = new Mock<IServiceProvider>();
            Mock<IServiceProvider> dummy_session_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_session = new Mock<IActiveSession>();
            stub_session.SetupGet(s => s.SessionServices).Returns(dummy_session_sp.Object);
            stub_session.SetupGet(s => s.IsAvailable).Returns(() => avail);
            Mock<IActiveSessionFeature> stub_as_feature = new Mock<IActiveSessionFeature>();
            stub_as_feature.SetupGet(s => s.ActiveSession).Returns(stub_session.Object);
            Mock<IFeatureCollection> stub_features_col = new Mock<IFeatureCollection>();
            stub_features_col.Setup(s => s.Get<IActiveSessionFeature>()).Returns(stub_as_feature.Object);
            Mock<HttpContext> stub_context = new Mock<HttpContext>();
            stub_context.SetupGet(s => s.RequestServices).Returns(dummy_req_sp.Object);
            stub_context.SetupGet(s => s.Features).Returns(stub_features_col.Object);
            Mock<IHttpContextAccessor> stub_accessor = new Mock<IHttpContextAccessor>();
            stub_accessor.SetupGet(s=>s.HttpContext).Returns(stub_context.Object);
            //Test case: ActiveSession is available
            //Act
            ActiveSessionRef sp_ref = new ActiveSessionRef(stub_accessor.Object);
            //Assess
            //TODO Add checks of ActiveSesssion and ActiveSessionInternal propertises
            Assert.True(sp_ref.IsFromSession);
            Assert.True(ReferenceEquals(dummy_session_sp.Object, sp_ref.Services));

            //Test case: ActiveSession is not available
            //Arrange more
            avail=false;
            //Act
            sp_ref = new ActiveSessionRef(stub_accessor.Object);
            //Assess
            Assert.False(sp_ref.IsFromSession);
            Assert.True(ReferenceEquals(dummy_req_sp.Object, sp_ref.Services));
        }
    }
}
