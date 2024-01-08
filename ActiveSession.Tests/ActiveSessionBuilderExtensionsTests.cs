using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        //Test case: use UseActiveSessions with required services added to the service container and default filter
        [Fact]
        public void UseActiveSessions_ServicesAdded()
        {
            //Arrange
            Mock<IActiveSessionStore> dummy_store = new Mock<IActiveSessionStore>();
            Mock<IServiceProvider> stub_sp= new Mock<IServiceProvider>();
            stub_sp.Setup(s=>s.GetService(typeof(IActiveSessionStore))).Returns(dummy_store.Object);
            MockedLoggerFactory factory_mock = new MockedLoggerFactory();
            stub_sp.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(factory_mock.LoggerFactory);
            Mock<IApplicationBuilder> mock_builder = new Mock<IApplicationBuilder>();
            mock_builder.SetupGet<IServiceProvider>(s=>s.ApplicationServices).Returns(stub_sp.Object);
            Func<RequestDelegate, RequestDelegate>? use_delegate=null;
            mock_builder.Setup(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                .Callback((Func<RequestDelegate, RequestDelegate> f)=>use_delegate=f).Returns(mock_builder.Object);
            //Act
            mock_builder.Object.UseActiveSessions();
            //Assess
            mock_builder.Verify(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()),Times.Once);
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
            Assert.True(ReferenceEquals(ActiveSessionBuilderExtensions.ACCEPTALL_FILTER, arg_values[0]));
            //End of fragile test
        }


        //Test case: use UseActiveSessions with required services not added to the service container
        [Fact]
        public void UseActiveSessions_ServicesNotAdded()
        {
            //Arrange
            Mock<IServiceProvider> stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(s => s.GetService(typeof(IActiveSessionStore))).Returns(null);
            MockedLoggerFactory factory_mock = new MockedLoggerFactory();
            stub_sp.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(factory_mock.LoggerFactory);
            Mock<IApplicationBuilder> mock_builder = new Mock<IApplicationBuilder>();
            mock_builder.SetupGet<IServiceProvider>(s => s.ApplicationServices).Returns(stub_sp.Object);
            mock_builder.Setup(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()));
            //Act
            mock_builder.Object.UseActiveSessions();
            //Assess
            mock_builder.Verify(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Never);
        }

        //TODO Test case: custom filter delegate installation
        [Fact]
        public void UseActiveSessions_DelegateFilter()
        {
            //Arrange
            Mock<IActiveSessionStore> dummy_store = new Mock<IActiveSessionStore>();
            Mock<IServiceProvider> stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(s => s.GetService(typeof(IActiveSessionStore))).Returns(dummy_store.Object);
            MockedLoggerFactory factory_mock = new MockedLoggerFactory();
            stub_sp.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(factory_mock.LoggerFactory);
            Mock<IApplicationBuilder> mock_builder = new Mock<IApplicationBuilder>();
            mock_builder.SetupGet<IServiceProvider>(s => s.ApplicationServices).Returns(stub_sp.Object);
            Func<RequestDelegate, RequestDelegate>? use_delegate = null;
            mock_builder.Setup(apb => apb.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                .Callback((Func<RequestDelegate, RequestDelegate> f) => use_delegate=f).Returns(mock_builder.Object);
            Func<HttpContext,Boolean> filter= context => context.Request.Path.Equals("/");
            //Act
            mock_builder.Object.UseActiveSessions(filter);
            //Assess
            //The test below may be fragile because of use of the specific knowledge about UseMiddleware method implementation
            Object use_delegate_target = use_delegate!.Target!;
            Type delegate_target_type = use_delegate_target.GetType();
            Object[] arg_values = (Object[])(((use_delegate_target.GetType()).GetField("args"))!.GetValue(use_delegate_target))!;
            Assert.True(ReferenceEquals(filter, arg_values[0]));
            //End of fragile test
        }

    }
}
