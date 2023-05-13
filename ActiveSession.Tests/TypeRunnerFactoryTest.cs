using MVVrus.AspNetCore.ActiveSession;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class TypeRunnerFactoryTest
    {
        public record Request1
        {
            public String Arg { get; set; } = "";
        }

        public class Result1
        {
            public String Value { get; init; }
            public Result1(String Value) {
                this.Value=Value;
            }
        }

        public class SpyRunner1:SpyRunnerBase<Result1>
        {
            public Request1 Request { get; init; }
            public SpyRunner1(Request1 Request) 
            {
                this.Request = Request;
            }
        }

        [Fact]
        public void CreateNoExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _)=>null);
            var result=new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner1), null);
            var request=new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal<Request1>(request, (runner as SpyRunner1)!.Request);
        }

        public interface ISpyInterface1
        {
            public String Value { get; }
        }
        public class SpyService : ISpyInterface1
        {
            public const String THE_VALUE= "SpyService";
            public string Value => THE_VALUE;
        }

        public class SpyRunner2: SpyRunner1 
        {
            public String Param1 { get; init; } = "";
            public int Param2 { get; init; }
            public ISpyInterface1? Param3 { get; init; }
            public SpyRunner2(Request1 Request, String Param1, int Param2, ISpyInterface1? Param3) : base(Request)
            {
                this.Param1=Param1;
                this.Param2=Param2;
                this.Param3=Param3;
            }
        }

        [Fact]
        public void CreateSpecifiedOnlyExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            var args = new Object[] { "ugu", 42, new SpyService() };
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner2), args);
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner2>(runner);
            Assert.Equal(request, (runner as SpyRunner2)!.Request);
            Assert.Equal((String)args[0], (runner as SpyRunner2)!.Param1);
            Assert.Equal((int)args[1], (runner as SpyRunner2)!.Param2);
            Assert.NotNull((runner as SpyRunner2)!.Param3);
            Assert.Equal(SpyService.THE_VALUE, (runner as SpyRunner2)!.Param3!.Value);
        }


        [Fact]
        public void CreateInjectedExtraParametersRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            stub_sp.Setup(x => x.GetService(typeof(ISpyInterface1))).Returns((Type _) => new SpyService());
            var args = new Object[] { "ugu", 42};
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner2), args);
            var request = new Request1 { Arg="value" };

            var runner = result.Create(request, stub_sp.Object);

            Assert.NotNull(runner);
            Assert.IsType<SpyRunner2>(runner);
            Assert.Equal(request, (runner as SpyRunner2)!.Request);
            Assert.Equal((String)args[0], (runner as SpyRunner2)!.Param1);
            Assert.Equal((int)args[1], (runner as SpyRunner2)!.Param2);
            Assert.NotNull((runner as SpyRunner2)!.Param3);
            Assert.Equal(SpyService.THE_VALUE, (runner as SpyRunner2)!.Param3!.Value);
        }

        public class SpyService2 : ISpyInterface1
        {
            public const String THE_VALUE = "SpyService2";
            public string Value => THE_VALUE;
        }

        [Fact]
        public void MissingExtraParameterCreateRunner()
        {
            var stub_sp = new Mock<IServiceProvider>();
            stub_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns((Type _) => null);
            var args = new Object[] { "ugu", 42};
            var result = new TypeRunnerFactory<Request1, Result1>(typeof(SpyRunner2), args);
            var request = new Request1 { Arg="value" };

            IActiveSessionRunner<Result1>? runner;
            
            Assert.Throws<InvalidOperationException>(() => { runner=result.Create(request, stub_sp.Object); });

            //Assert.NotNull(runner);
            //Assert.IsType<SpyRunner2>(runner);
            //Assert.Equal(request, (runner as SpyRunner2)!.Request);
            //Assert.Equal((String)args[0], (runner as SpyRunner2)!.Param1);
            //Assert.Equal((int)args[1], (runner as SpyRunner2)!.Param2);
            //Assert.Null((runner as SpyRunner2)!.Param3);
        }

    }
}
