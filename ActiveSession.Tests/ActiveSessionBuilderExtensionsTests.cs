using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
        [Fact]
        public void UseActiveSessions_ServicesAdded()
        {
            Mock<IActiveSessionStore> dummy_store = new Mock<IActiveSessionStore>();
            Mock<IServiceProvider> stub_sp= new Mock<IServiceProvider>();
            stub_sp.Setup(s=>s.GetService(typeof(IActiveSessionStore))).Returns(dummy_store.Object);
            Mock<IApplicationBuilder> mock_builder = new Mock<IApplicationBuilder>();
            mock_builder.SetupGet<IServiceProvider>(s=>s.ApplicationServices).Returns(stub_sp.Object);
            Func<RequestDelegate, RequestDelegate>? use_delegate=null;
            mock_builder.Setup(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                .Callback((Func<RequestDelegate, RequestDelegate> f)=>use_delegate=f).Returns(mock_builder.Object);

            mock_builder.Object.UseActiveSessions();

            mock_builder.Verify(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()),Times.Once);
            //The test below may be fragile because of use of the specific knowledge about UseMiddleware method implementation
            Assert.NotNull(use_delegate);
            Object? use_delegate_target = use_delegate!.Target;
            Assert.NotNull(use_delegate_target);
            Type delegate_target_type = use_delegate_target.GetType();
            FieldInfo? middleware_field = delegate_target_type.GetField("middleware");
            Assert.NotNull(middleware_field);
            Type? middleware = (Type?)(middleware_field!.GetValue(use_delegate_target));
            Assert.Equal(typeof(ActiveSessionMiddleware), middleware);
            //End of fragile test
        }

        [Fact]
        public void UseActiveSessions_ServicesNotAdded()
        {
            Mock<IServiceProvider> stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(s => s.GetService(typeof(IActiveSessionStore))).Returns(null);
            Mock<IApplicationBuilder> mock_builder = new Mock<IApplicationBuilder>();
            mock_builder.SetupGet<IServiceProvider>(s => s.ApplicationServices).Returns(stub_sp.Object);
            mock_builder.Setup(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()));

            mock_builder.Object.UseActiveSessions();

            mock_builder.Verify(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Never);
        }

    }
}
