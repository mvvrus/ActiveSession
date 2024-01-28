using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using static ActiveSession.Tests.SimulatedActiveSessionConfiguration;
using MVVrus.AspNetCore.ActiveSession;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace ActiveSession.Tests
{
    public class ActiveSessionServiceCollectionExtensionsTests
    {
        int CountServiceImplementations(IServiceCollection ServiceDescriptors, Type ServiceType)
        {
            return ServiceDescriptors.Where(sd => sd.ServiceType==ServiceType).Count();
        }

        void CheckInfrastructure(IServiceCollection Services, Boolean IsConfigDelegateUsed)
        {
            Assert.Equal(1, CountServiceImplementations(Services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(Services, typeof(IRunnerManagerFactory)));
            Assert.Equal(1, CountServiceImplementations(Services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(IsConfigDelegateUsed ? 1 : 0, CountServiceImplementations(Services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));
        }

        //Test group: ActiveSession infrastructure services registratiom
        [Fact(DisplayName = "AddActiveSessionInfrastructure method")]
        public void AcitiveSessionInfrastructure()
        {
            const String HOST1 = "host1";
            const String HOST2 = "host2";
            const String PREFIX1 = "prefix1";
            var dummy_memory_cache = new Mock<IMemoryCache>();
            IConfiguration config = CreateSimulatedActiveSessionCongfiguration(new { HostId = HOST1 });
            IServiceCollection services;
            IServiceProvider sp;

            //Test case: single AddActiveSessionInfrastructure w/o configuration delegate
            //Arrange
            services=new ServiceCollection();
            services.AddSingleton<IMemoryCache>(dummy_memory_cache.Object);
            services.AddSingleton<IConfiguration>(config);
            //Act
            ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure(services, null);
            //Assess
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IRunnerManagerFactory)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IHttpContextAccessor)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(ActiveSessionServiceProviderRef)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionService<>)));
            Assert.Equal(0, CountServiceImplementations(services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));
            //Check that value from the IConfiguration is accepted
            sp=services.BuildServiceProvider();
            Assert.Equal(HOST1, sp.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.HostId);

            //Test case: single AddActiveSessionInfrastructure with configuration delegate
            //Arrange
            services=new ServiceCollection();
            services.AddSingleton<IMemoryCache>(dummy_memory_cache.Object);
            services.AddSingleton<IConfiguration>(config);
            //Act
            ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure(services, o => o.Prefix=PREFIX1);
            //Assss
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));

            //Test case repeated AddActiveSessionInfrastructure w/o configuration delegate (already arranged)
            //Act
            ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure(services, null);
            services.AddSingleton<IConfiguration>(config);
            //Assess
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IHttpContextAccessor)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(ActiveSessionServiceProviderRef)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionService<>)));

            //Test case: repeated AddActiveSessionInfrastructure with configuration delegate (already arranged)
            //Act
            ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure(services, o => o.HostId=HOST2);
            //Assess
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(2, CountServiceImplementations(services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IHttpContextAccessor)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(ActiveSessionServiceProviderRef)));
            Assert.Equal(1, CountServiceImplementations(services, typeof(IActiveSessionService<>)));
            //Check that value from configuration delegates are accepted and a value from the IConfiguration is overriden
            sp=services.BuildServiceProvider();
            Assert.Equal(PREFIX1, sp.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.Prefix);
            Assert.Equal(HOST2, sp.GetRequiredService<IOptions<ActiveSessionOptions>>().Value.HostId);
        }

        const String REQ_ARG = "Request1 argument";
        static RunnerId REQ_ID = ("TEST_SESSION", 42);

        [Fact]
        public void DelegateFactory_OneParam_NoConfig()
        {
            Func<Request1, IRunner<Result1>> factory = x => new SpyRunner1_2(x);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory);

            CheckInfrastructure(services, false);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            CheckFactory(REQ_ARG, rf);
        }

        [Fact]
        public void DelegateFactory_OneParam_Config()
        {
            Func<Request1, IRunner<Result1>> factory = x => new SpyRunner1_2(x);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory, o => { });

            CheckInfrastructure(services, true);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            CheckFactory(REQ_ARG, rf);
        }

        [Fact]
        public void DelegateFactory_TwoParams_NoConfig()
        {
            Func<Request1, IServiceProvider, IRunner<Result1>> factory = (x, sp) => new SpyRunner1_2(x);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory);

            CheckInfrastructure(services, false);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            CheckFactory(REQ_ARG, rf);
        }

        [Fact]
        public void DelegateFactory_TwoParams_Config()
        {
            Func<Request1, IServiceProvider, IRunner<Result1>> factory = (x, sp) => new SpyRunner1_2(x);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory, o => { });

            CheckInfrastructure(services, true);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            CheckFactory(REQ_ARG, rf);
        }

        [Fact]
        public void DelegateFactory_ThreeParams_NoConfig()
        {
            Func<Request1, IServiceProvider, RunnerId, IRunner<Result1>> factory = (x, sp, id) => new SpyRunner1_2(x,id);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory);

            CheckInfrastructure(services, false);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            CheckFactory(REQ_ARG, rf, REQ_ID);
        }

        [Fact]
        public void DelegateFactory_ThreeParams_Config()
        {
            Func<Request1, IServiceProvider, RunnerId, IRunner<Result1>> factory = (x, sp, id) => new SpyRunner1_2(x, id);
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions(factory, o => { });

            CheckInfrastructure(services, true);
            DelegateRunnerFactory<Request1, Result1> rf = MakeAndCheckDelegateFactory(services);
            CheckFactory(REQ_ARG, rf, REQ_ID);
        }

        private static DelegateRunnerFactory<Request1, Result1> MakeAndCheckDelegateFactory(IServiceCollection services)
        {
            IServiceProvider sp = services.BuildServiceProvider();
            IServiceProviderIsService spis = sp.GetRequiredService<IServiceProviderIsService>();
            Assert.True(spis.IsService(typeof(IRunnerFactory<Request1, Result1>)));
            IRunnerFactory<Request1, Result1> rf = sp.GetRequiredService<IRunnerFactory<Request1, Result1>>();
            Assert.IsType<DelegateRunnerFactory<Request1, Result1>>(rf);
            return (DelegateRunnerFactory<Request1, Result1>)rf;
        }

        private static void CheckFactory(String RequestArg, DelegateRunnerFactory<Request1, Result1> rf, RunnerId Id=default)
        {
            Mock<IServiceProvider> mock_sp = new Mock<IServiceProvider>();
            mock_sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns(null);
            IRunner<Result1> runner = rf.Create(new Request1  { Arg = RequestArg }, mock_sp.Object, Id);
            Assert.IsType<SpyRunner1_2>(runner);
            Assert.Equal(RequestArg, ((SpyRunner1_2)runner).Request.Arg);
            Assert.Equal(Id, ((SpyRunner1_2)runner).Id);
        }

        //================= Tests for TypeRunnerFactory-based factories ==================================================
        private static void CheckTypeFactories(
            IServiceCollection Services,
            Type[] RequestTypes,
            Type[] RunnerResultTypes,
            Type implementer,
            Object[]? ExtraParams = null,
            Int32[]? ReqParams = null
        )
        {
            Int32[] req_params = ReqParams??new Int32[RequestTypes.Length];
            if (ReqParams==null) Array.Fill(req_params, 1);
            Type[] type_args = new Type[2];
            foreach (Type result_type in RunnerResultTypes) {
                for (int nreq=0;nreq<RequestTypes.Length;nreq++) {
                    Type request_type = RequestTypes[nreq];
                    type_args[0]=request_type;
                    type_args[1]=result_type;
                    Type factory_service_type = typeof(IRunnerFactory<,>).MakeGenericType(type_args);
                    Assert.Single(Services, x => x.ServiceType==factory_service_type);
                    ServiceDescriptor sd = Services.Where(x => x.ServiceType==factory_service_type).First();
                    Assert.NotNull(sd.ImplementationFactory);
                    Assert.IsType<ActiveSessionServiceCollectionExtensions.FactoryDelegateTarget>(sd.ImplementationFactory.Target);
                    var fdt = (ActiveSessionServiceCollectionExtensions.FactoryDelegateTarget)sd.ImplementationFactory.Target;
                    Assert.Equal(implementer, fdt.RunnerResultType);
                    Assert.Equal(req_params[nreq], fdt.NumberOfRequiredParams); 
                    if (ExtraParams!=null) {
                        Assert.Equal(ExtraParams!.Length, fdt.ExtraArguments.Length);
                        for(int i=0;i<ExtraParams!.Length;i++) {
                            Assert.IsType(ExtraParams![i].GetType(), fdt.ExtraArguments[i]);
                            Assert.True(ExtraParams![i].Equals(fdt.ExtraArguments[i]));
                        }

                    }
                    else
                        Assert.Empty(fdt.ExtraArguments);
                    Assert.Equal(typeof(TypeRunnerFactory<,>).MakeGenericType(type_args), fdt.FactoryImplObjectConstructor.DeclaringType);
                }
            }
        }

        class FactoryDelegateTargetTestObject
        {
            public Type RunnerType { get; init; }
            public Object[]? Params { get; init; }
            public ILoggerFactory? LoggerFactory { get; init; }
            public Int32 NumberOfRequiredParams { get; init; }

            public FactoryDelegateTargetTestObject(Type RunnerType, Object[]? Params, Int32 NumberOfRequiredParams, ILoggerFactory? LoggerFactory)
            {
                this.RunnerType=RunnerType;
                this.Params=Params;
                this.LoggerFactory=LoggerFactory;
                this.NumberOfRequiredParams=NumberOfRequiredParams;
            }
        }

        [Fact(DisplayName= "Runner factory(type-based) constructor params")]
        public void FactoryDelegateTarget_InvocationParams()
        {
            ILoggerFactory dummy_logger_factory = new Mock<ILoggerFactory>().Object;
            Mock<IServiceProvider> mock_sp = new Mock<IServiceProvider>();
            Expression<Func<IServiceProvider, Object?>> get_lf_service = (IServiceProvider x) => x.GetService(typeof(ILoggerFactory));
            mock_sp.Setup(get_lf_service).Returns(dummy_logger_factory);
            ConstructorInfo ci = typeof(FactoryDelegateTargetTestObject).GetConstructors().FirstOrDefault()!;
            Object[] extra_params = new Object[] {"Test", 1 };

            var fdt = new ActiveSessionServiceCollectionExtensions.FactoryDelegateTarget(ci,typeof(String), 42, extra_params);
            Object result = fdt.Invoke(mock_sp.Object);

            mock_sp.Verify(get_lf_service, Times.Once);
            Assert.IsType<FactoryDelegateTargetTestObject>(result);
            FactoryDelegateTargetTestObject fdto = (FactoryDelegateTargetTestObject)result;
            Assert.NotNull(fdto.Params);
            Assert.Equal(typeof(String), fdto.RunnerType);
            Assert.Equal(extra_params, fdto.Params);
            Assert.Equal(dummy_logger_factory, fdto.LoggerFactory);
            Assert.Equal(42, fdto.NumberOfRequiredParams);
        }


        [Fact(DisplayName = "Runner factory(type-based) service resolution")]
        public void FactoryType()
        {
            //Arrrange
            IServiceCollection services = new ServiceCollection();
            //Act
            services.AddActiveSessions<SpyRunner1>();
            //Assess
            Type[] type_args = new Type[2];
            type_args[0]=typeof(Request1);
            type_args[1]=typeof(Result1);
            Type factory_service_type = typeof(IRunnerFactory<,>).MakeGenericType(type_args);
            Assert.Single(services, x => x.ServiceType==factory_service_type);
            ServiceDescriptor sd = services.Where(x => x.ServiceType==factory_service_type).First();
            Assert.NotNull(sd.ImplementationFactory);
            Assert.IsType<ActiveSessionServiceCollectionExtensions.FactoryDelegateTarget>(sd.ImplementationFactory.Target);
            var fdt = (ActiveSessionServiceCollectionExtensions.FactoryDelegateTarget)sd.ImplementationFactory.Target;
            Object result = fdt.Invoke(services.BuildServiceProvider());
            Type factory_type = typeof(TypeRunnerFactory<,>).MakeGenericType(type_args);
            Assert.IsType(factory_type, result);
        }

        [Fact]
        public void TypeFactory1Constructor_NoConfig()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner1>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services,new Type[]{typeof (Request1)}, new Type[] {typeof( Result1)}, typeof(SpyRunner1));
        }

        [Fact]
        public void TypeFactory1Constructor_Config()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner1>(o=>o.HostId="unknown");

            CheckInfrastructure(services, true);
            CheckTypeFactories(services, new Type[] { typeof(Request1) }, new Type[] { typeof(Result1) }, typeof(SpyRunner1));
        }

        [Fact]
        public void TypeFactory3Constructors_NoAttribute()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner2>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(Request1), typeof(String), typeof(int) }, new Type[] { typeof(Result1) }, typeof(SpyRunner2));
        }

        [Fact]
        public void TypeFactory_NumerRequiredParams()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner2_2>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, 
                new Type[] { typeof(Request1), typeof(String), typeof(int) }, 
                new Type[] { typeof(Result1) }, 
                typeof(SpyRunner2_2),
                null, 
                new Int32[] {1,2,1});
        }

        [Fact]
        public void TypeFactory_AmbiguousConstructors()
        {
            IServiceCollection services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(()=>services.AddActiveSessions<SpyRunner2_2a>());

        }

        [Fact]
        public void TypeFactory3Constructors_AttributeTrue()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner3>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(String) }, new Type[] { typeof(Result1) }, typeof(SpyRunner3));
        }

        [Fact]
        public void TypeFactory3Constructors_AttributeFalse()
        {

            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner4>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(Request1), typeof(int) }, new Type[] { typeof(Result1) }, typeof(SpyRunner4));
        }

        [Fact]
        public void TypeFactoryThreeConstructors_ActivatorUtilitiesConstructorAttribute()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner5>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(int) }, new Type[] { typeof(Result1) }, typeof(SpyRunner5));

        }

        [Fact]
        public void TypeFactory1Constructor_2Interfaces()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner6>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(Request1) }, new Type[] { typeof(Result1), typeof(String) }, typeof(SpyRunner6));
        }

        [Fact]
        public void TypeFactory3Constructors_2Interfaces()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddActiveSessions<SpyRunner7>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(Request1), typeof(String), typeof(int) }, new Type[] { typeof(Result1), typeof(String) }, typeof(SpyRunner7));

        }

        [Fact]
        public void TypeFactory1Constructor_Params()
        {
            IServiceCollection services = new ServiceCollection();
            String StringParam = "StringParam";
            int IntParam = 1;

            services.AddActiveSessions<SpyRunner1>(StringParam,IntParam);

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(Request1) }, new Type[] { typeof(Result1) }, 
                typeof(SpyRunner1), new Object[] {StringParam,IntParam});
        }

        [Fact]
        public void TypeFactory3Constructors_2Interfaces_Params()
        {
            IServiceCollection services = new ServiceCollection();
            String StringParam = "StringParam";
            int IntParam = 1;

            services.AddActiveSessions<SpyRunner7>(StringParam, IntParam);

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(Request1), typeof(String), typeof(int) }, new Type[] { typeof(Result1), typeof(String) },
                typeof(SpyRunner7), new Object[] { StringParam, IntParam });
        }

    }
}
