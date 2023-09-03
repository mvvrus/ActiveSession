using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static ActiveSession.Tests.SimulatedActiveSessionConfiguration;
using MVVrus.AspNetCore.ActiveSession;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;

namespace ActiveSession.Tests
{
    public class ActiveSessionServiceCollectionExtensionsTests
    {
        public record Request1
        {
            public String Arg { get; set; } = "";
        }

        public class Result1
        {
            public String Value { get; init; }
            public Result1(String Value)
            {
                this.Value=Value;
            }
        }

        public class SpyRunner1 : SpyRunnerBase<Result1>
        {
            public Request1 Request { get; init; }
            public SpyRunner1(Request1 Request)
            {
                this.Request=Request;
            }
        }

        int CountServiceImplementations(IServiceCollection ServiceDescriptors, Type ServiceType)
        {
            return ServiceDescriptors.Where(sd=>sd.ServiceType==ServiceType).Count();
        }

        void CheckInfrastructure(IServiceCollection Services, Boolean IsConfigDelegateUsed)
        {
            Assert.Equal(1, CountServiceImplementations(Services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(Services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(IsConfigDelegateUsed?1:0, CountServiceImplementations(Services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));
        }

        [Fact]
        public void AcitiveSessionInfrastructure()
        {
            const String HOST1 = "host1";
            const String HOST2 = "host2";
            const String PREFIX1 = "prefix1";
            var fake_memory_cache = new Mock<IMemoryCache>();
            IConfiguration config = CreateSimulatedActiveSessionCongfiguration(new { HostId = HOST1 });
            IServiceCollection services;
            IServiceProvider sp;

            //Check single AddActiveSessionInfrastructure w/o configuration delegate
            services= new ServiceCollection();
            services.AddSingleton<IMemoryCache>(fake_memory_cache.Object);
            services.AddSingleton<IConfiguration>(config);
            ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure(services, null);
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(0, CountServiceImplementations(services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));

            //Check that value from the IConfiguration is accepted
            sp=services.BuildServiceProvider();
            Assert.Equal(HOST1, sp.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.HostId);

            //Check single AddActiveSessionInfrastructure with configuration delegate
            services=new ServiceCollection();
            services.AddSingleton<IMemoryCache>(fake_memory_cache.Object);
            services.AddSingleton<IConfiguration>(config);
            ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure(services, o=>o.Prefix=PREFIX1);
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));

            //Check repeated AddActiveSessionInfrastructure w/o configuration delegate
            ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure(services, null);
            services.AddSingleton<IConfiguration>(config);
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));

            //Check repeated AddActiveSessionInfrastructure with configuration delegate
            ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure(services, o => o.HostId=HOST2);
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(2, CountServiceImplementations(services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));

            //Check that value from configuration delegates are ccepted and a value from the IConfiguration is overriden
            sp=services.BuildServiceProvider();
            Assert.Equal(PREFIX1, sp.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.Prefix);
            Assert.Equal(HOST2, sp.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.HostId);
        }

        const String REQ_ARG = "Request1 argument";

        [Fact]
        public void DelegateFactory_OneParam_NoConfig()
        {
            Func<Request1, IActiveSessionRunner<Result1>> factory = x => new SpyRunner1(x);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory);

            CheckInfrastructure(services, false);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            CheckSPIndependentFactory(REQ_ARG, rf);
        }

        [Fact]
        public void DelegateFactory_OneParam_Config()
        {
            Func<Request1, IActiveSessionRunner<Result1>> factory = x => new SpyRunner1(x);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory, o => { });

            CheckInfrastructure(services, true);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            CheckSPIndependentFactory(REQ_ARG, rf);
        }

        [Fact]
        public void DelegateFactory_TwoParams_NoConfig()
        {
            Func<Request1, IServiceProvider, IActiveSessionRunner<Result1>> factory = (x,sp) => new SpyRunner1(x);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory);

            CheckInfrastructure(services, false);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            Assert.True(factory==rf.Factory);
        }

        [Fact]
        public void DelegateFactory_TwoParams_Config()
        {
            Func<Request1, IServiceProvider, IActiveSessionRunner<Result1>> factory = (x, sp) => new SpyRunner1(x);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory, o => { });

            CheckInfrastructure(services, true);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            Assert.True(factory==rf.Factory);
        }

        private static DelegateRunnerFactory<Request1, Result1> MakeAndCheckDelegateFactory(IServiceCollection services)
        {
            IServiceProvider sp = services.BuildServiceProvider();
            IServiceProviderIsService spis = sp.GetRequiredService<IServiceProviderIsService>();
            Assert.True(spis.IsService(typeof(IActiveSessionRunnerFactory<Request1, Result1>)));
            IActiveSessionRunnerFactory<Request1, Result1> rf = sp.GetRequiredService<IActiveSessionRunnerFactory<Request1, Result1>>();
            Assert.IsType<DelegateRunnerFactory<Request1, Result1>>(rf);
            return (DelegateRunnerFactory<Request1, Result1>)rf;
        }

        private static void CheckSPIndependentFactory(String RequestArg, DelegateRunnerFactory<Request1, Result1> rf)
        {
            Mock<IServiceProvider> mock_sp = new Mock<IServiceProvider>();
            mock_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns(null);
            IActiveSessionRunner<Result1> runner =
              rf.Factory(new Request1 { Arg=RequestArg }, mock_sp.Object);
            Assert.IsType<SpyRunner1>(runner);
            Assert.Equal(RequestArg, ((SpyRunner1)runner).Request.Arg);
            mock_sp.Verify(x => x.GetService(It.IsAny<Type>()), Times.Never());
        }

        private static void CheckTypeFactories(IServiceCollection Services, Type[] RequestTypes, Type[] RunnerResultTypes)
        {
            Type[] type_args = new Type[2];
            foreach (Type result_type in RunnerResultTypes) {
                foreach (Type request_type in RequestTypes) {
                    type_args[0]=request_type;
                    type_args[1]=result_type;
                    Type factory_service_type = typeof(IActiveSessionRunnerFactory<,>).MakeGenericType(type_args);
                    Assert.Single(Services, x => x.ServiceType==factory_service_type);
                    ServiceDescriptor sd = Services.Where(x=>x.ServiceType==factory_service_type).First();
                    Assert.NotNull(sd.ImplementationFactory);
                    Assert.IsType<ActiveSessionServiceCollectionExtensions.FactoryDelegateTarget>(sd.ImplementationFactory.Target);
                }
            }
        }

        [Fact]
        public void TypeFactorySimple_NoConfig()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner1>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services,new Type[]{typeof (Request1)}, new Type[] {typeof( Result1)});
        }

        [Fact]
        public void TypeFactorySimple_Config()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner1>(o=>o.HostId="unknown");

            CheckInfrastructure(services, true);
            CheckTypeFactories(services, new Type[] { typeof(Request1) }, new Type[] { typeof(Result1) });
        }

        /*
        public void TypeFactoryThreeConstructors_NoConstructorAttribute()
        {

        }

        public void TypeFactoryThreeConstructors_ConstructorAttribute_True()
        {

        }

        public void TypeFactoryThreeConstructors_ConstructorAttribute_False()
        {

        }

        public void TypeFactoryThreeConstructors_ConstructorAttribute_True_And_False()
        {

        }

        public void TypeFactoryThreeConstructors_ActivatorUtilitiesConstructorAttribute()
        {

        }

        public void TypeFactorySimple_InheritTwoInterfaces()
        {

        }

        public void TypeFactoryThreeConstructors_InheritTwoInterfaces()
        {

        }

         */

    }
}
