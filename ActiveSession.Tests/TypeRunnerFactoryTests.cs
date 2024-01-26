using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession;
using MVVrus.AspNetCore.ActiveSession.Internal;

namespace ActiveSession.Tests
{
    public class TypeRunnerFactoryTests
    {

        [Fact]
        public void CreateNoExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _)=>null);
            var result=new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner1), null, MakeLoggerFactory());
            var request=new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object,default);
            //TODO Add test with non-default runner

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal<Request1>(request, (runner as SpyRunner1)!.Request);
        }

        [Fact]
        public void CreateSpecifiedOnlyExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            var args = new Object[] { "ugu", 42, new SpyService() };
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner8), args, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object, default);
            //TODO Add test with non-default runner

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner8>(runner);
            Assert.Equal(request, (runner as SpyRunner8)!.Request);
            Assert.Equal((String)args[0], (runner as SpyRunner8)!.Param1);
            Assert.Equal((int)args[1], (runner as SpyRunner8)!.Param2);
            Assert.NotNull((runner as SpyRunner8)!.Param3);
            Assert.Equal(SpyService.THE_VALUE, (runner as SpyRunner8)!.Param3!.Value);
        }


        [Fact]
        public void CreateInjectedExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            stub_sp.Setup(x => x.GetService(typeof(ISpyInterface1))).Returns((Type _) => new SpyService());
            var args = new Object[] { "ugu", 42};
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner8), args, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object, default);
            //TODO Add test with non-default runner

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner8>(runner);
            Assert.Equal(request, (runner as SpyRunner8)!.Request);
            Assert.Equal((String)args[0], (runner as SpyRunner8)!.Param1);
            Assert.Equal((int)args[1], (runner as SpyRunner8)!.Param2);
            Assert.NotNull((runner as SpyRunner8)!.Param3);
            Assert.Equal(SpyService.THE_VALUE, (runner as SpyRunner8)!.Param3!.Value);
        }

        [Fact]
        public void MissingExtraParameterCreateRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            var args = new Object[] { "ugu", 42};
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner8), args, MakeLoggerFactory());
            var request = new Request1 { Arg="value" };

            IRunner<Result1>? runner;
            
            Assert.Throws<InvalidOperationException>(() => { runner=result.Create(request, stub_sp.Object, default); });
            //TODO Add test with non-default runner
        }

        ILoggerFactory MakeLoggerFactory()
        {
            MockedLoggerFactory logger_factory = new MockedLoggerFactory();
            logger_factory.MonitorLoggerCategory(ActiveSessionConstants.LOGGING_CATEGORY_NAME);
            return logger_factory.LoggerFactory;
        }
    }
}
