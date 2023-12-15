using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ActiveSessionContextExtensionTests
    {
        //Test case: no ActiveSessionFeature in the context
        [Fact]
        public void GetActiveSession_NoFeature()
        {
            //Arrange
            Mock<IFeatureCollection> stub_feature_col=new Mock<IFeatureCollection>();
            stub_feature_col.Setup(s => s.Get<IActiveSessionFeature>()).Returns((IActiveSessionFeature?)null);
            Mock<HttpContext> stub_context = new Mock<HttpContext>();
            stub_context.SetupGet(s=>s.Features).Returns(stub_feature_col.Object);
            //Act & assess
            Assert.Null(stub_context.Object.GetActiveSession());
        }

        [Fact]
        public void GetActiveSession_NullActiveSession()
        {
            //Arrange
            Mock<IActiveSessionFeature> stub_feature = new Mock<IActiveSessionFeature>();
            stub_feature.SetupGet(s => s.ActiveSession).Returns((IActiveSession)null!);
            Mock<IFeatureCollection> stub_feature_col = new Mock<IFeatureCollection>();
            stub_feature_col.Setup(s => s.Get<IActiveSessionFeature>()).Returns(stub_feature.Object);
            Mock<HttpContext> stub_context = new Mock<HttpContext>();
            stub_context.SetupGet(s => s.Features).Returns(stub_feature_col.Object);
            //Act & assess
            Assert.Null(stub_context.Object.GetActiveSession());
        }

        [Fact]
        public void GetActiveSession_UnavailableActiveSession()
        {
            //Arrange
            Mock<IActiveSession> stub_session = new Mock<IActiveSession>();
            stub_session.SetupGet(s => s.IsAvailable).Returns(false);
            Mock<IActiveSessionFeature> stub_feature = new Mock<IActiveSessionFeature>();
            stub_feature.SetupGet(s => s.ActiveSession).Returns(stub_session.Object);
            Mock<IFeatureCollection> stub_feature_col = new Mock<IFeatureCollection>();
            stub_feature_col.Setup(s => s.Get<IActiveSessionFeature>()).Returns(stub_feature.Object);
            Mock<HttpContext> stub_context = new Mock<HttpContext>();
            stub_context.SetupGet(s => s.Features).Returns(stub_feature_col.Object);
            //Act & assess
            Assert.Null(stub_context.Object.GetActiveSession());
        }

        [Fact]
        public void GetActiveSession_ActiveSessionOK()
        {
            //Arrange
            Mock<IActiveSession> stub_session = new Mock<IActiveSession>();
            stub_session.SetupGet(s => s.IsAvailable).Returns(true);
            Mock<IActiveSessionFeature> stub_feature = new Mock<IActiveSessionFeature>();
            stub_feature.SetupGet(s => s.ActiveSession).Returns(stub_session.Object);
            Mock<IFeatureCollection> stub_feature_col = new Mock<IFeatureCollection>();
            stub_feature_col.Setup(s => s.Get<IActiveSessionFeature>()).Returns(stub_feature.Object);
            Mock<HttpContext> stub_context = new Mock<HttpContext>();
            stub_context.SetupGet(s => s.Features).Returns(stub_feature_col.Object);
            //Act & assess
            Assert.Equal(stub_session.Object, stub_context.Object.GetActiveSession());
        }

    }
}
