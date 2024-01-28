using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession;
using MVVrus.AspNetCore.ActiveSession.Internal;

namespace ActiveSession.Tests
{
    public class TypeRunnerFactoryTests
    {
        static RunnerId REQ_ID = ("TEST_SESSION", 42);

        //Test case: invalid number of required parameters
        [Fact]
        public void InvalidNumberOfRequiredParametrs()
        {
            //Arrange
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            //Act&Assess
            Assert.Throws<ArgumentOutOfRangeException>(()=>new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner1_2), null, 10, MakeLoggerFactory()));
        }

        [Fact]
        public void CreateNoExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _)=>null);
            var result=new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner1_2), null, 1,MakeLoggerFactory());
            var request=new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object,REQ_ID);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1_2>(runner);
            Assert.Equal(request, (runner as SpyRunner1_2)!.Request);
            Assert.Equal(default(RunnerId), (runner as SpyRunner1_2)!.Id);
        }

        [Fact]
        public void CreateSpecifiedOnlyExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            var args = new Object[] { "ugu", 42, new SpyService() };
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner8_2), args, 1, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object, REQ_ID);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner8_2>(runner);
            Assert.Equal(request, (runner as SpyRunner8_2)!.Request);
            Assert.Equal((String)args[0], (runner as SpyRunner8_2)!.Param1);
            Assert.Equal((int)args[1], (runner as SpyRunner8_2)!.Param2);
            Assert.NotNull((runner as SpyRunner8_2)!.Param3);
            Assert.Equal(SpyService.THE_VALUE, (runner as SpyRunner8_2)!.Param3!.Value);
            Assert.Equal(default(RunnerId), (runner as SpyRunner8_2)!.Id);
        }

        [Fact]
        public void CreateInjectedExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            stub_sp.Setup(x => x.GetService(typeof(ISpyInterface1))).Returns((Type _) => new SpyService());
            var args = new Object[] { "ugu", 42};
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner8_2), args, 1, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object, REQ_ID);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner8_2>(runner);
            Assert.Equal(request, (runner as SpyRunner8_2)!.Request);
            Assert.Equal((String)args[0], (runner as SpyRunner8_2)!.Param1);
            Assert.Equal((int)args[1], (runner as SpyRunner8_2)!.Param2);
            Assert.NotNull((runner as SpyRunner8_2)!.Param3);
            Assert.Equal(SpyService.THE_VALUE, (runner as SpyRunner8_2)!.Param3!.Value);
            Assert.Equal(default(RunnerId), (runner as SpyRunner8_2)!.Id);
        }

        [Fact]
        public void MissingExtraParameterCreateRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            var args = new Object[] { "ugu", 42};
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner8_2), args, 1, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            IRunner<Result1>? runner;
            
            Assert.Throws<InvalidOperationException>(() => { runner=result.Create(request, stub_sp.Object, REQ_ID); });
        }

        [Fact]
        public void CreateNoExtraParametersRunnerWithId()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner1_2), null, 2, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object, REQ_ID);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1_2>(runner);
            Assert.Equal(request, (runner as SpyRunner1_2)!.Request);
            Assert.Equal(REQ_ID, (runner as SpyRunner1_2)!.Id);
        }

        [Fact]
        public void CreateSpecifiedOnlyExtraParametersRunnerWithId()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            var args = new Object[] { "ugu", 42, new SpyService() };
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner8_2), args, 2, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object, REQ_ID);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner8_2>(runner);
            Assert.Equal(request, (runner as SpyRunner8_2)!.Request);
            Assert.Equal((String)args[0], (runner as SpyRunner8_2)!.Param1);
            Assert.Equal((int)args[1], (runner as SpyRunner8_2)!.Param2);
            Assert.NotNull((runner as SpyRunner8_2)!.Param3);
            Assert.Equal(SpyService.THE_VALUE, (runner as SpyRunner8_2)!.Param3!.Value);
            Assert.Equal(REQ_ID, (runner as SpyRunner8_2)!.Id);
        }

        [Fact]
        public void CreateInjectedExtraParametersRunnerWithId()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            stub_sp.Setup(x => x.GetService(typeof(ISpyInterface1))).Returns((Type _) => new SpyService());
            var args = new Object[] { "ugu", 42 };
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner8_2), args, 2, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object, REQ_ID);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner8_2>(runner);
            Assert.Equal(request, (runner as SpyRunner8_2)!.Request);
            Assert.Equal((String)args[0], (runner as SpyRunner8_2)!.Param1);
            Assert.Equal((int)args[1], (runner as SpyRunner8_2)!.Param2);
            Assert.NotNull((runner as SpyRunner8_2)!.Param3);
            Assert.Equal(SpyService.THE_VALUE, (runner as SpyRunner8_2)!.Param3!.Value);
            Assert.Equal(REQ_ID, (runner as SpyRunner8_2)!.Id);
        }

        ILoggerFactory MakeLoggerFactory()
        {
            MockedLoggerFactory logger_factory = new MockedLoggerFactory();
            logger_factory.MonitorLoggerCategory(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
            return logger_factory.LoggerFactory;
        }
    }
}
