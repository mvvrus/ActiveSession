using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;

namespace ActiveSession.Tests
{
    public static class ServiceCollectionExtensionTestUtils
    {
        public static int CountServiceImplementations(IServiceCollection ServiceDescriptors, Type ServiceType)
        {
            return ServiceDescriptors.Where(sd => sd.ServiceType==ServiceType).Count();
        }

        public static void CheckInfrastructure(IServiceCollection Services, Boolean IsConfigDelegateUsed)
        {
            Assert.Equal(1, CountServiceImplementations(Services, typeof(IActiveSessionStore)));
            Assert.Equal(1, CountServiceImplementations(Services, typeof(IRunnerManagerFactory)));
            Assert.Equal(1, CountServiceImplementations(Services, typeof(IConfigureOptions<ActiveSessionOptions>)));
            Assert.Equal(IsConfigDelegateUsed ? 1 : 0, CountServiceImplementations(Services, typeof(IPostConfigureOptions<ActiveSessionOptions>)));
        }

        public static void CheckTypeFactories(
            IServiceCollection Services,
            Type[] RequestTypes,
            Type[] RunnerResultTypes,
            Type implementer,
            Object[]? ExtraParams = null,
            Int32[]? ReqParams = null
        )
        {
            //Int32[] req_params = ReqParams??new Int32[RequestTypes.Length];
            if(ReqParams!=null && ReqParams.Length!=RequestTypes.Length) 
                throw new Exception("Invalid test data: lengths of RequestTypes and ReqParams arrays mismatch.");
            //if(ReqParams==null) Array.Fill(req_params, 1);
            Type[] type_args = new Type[2];
            foreach(Type result_type in RunnerResultTypes) {
                for(int nreq = 0; nreq<RequestTypes.Length; nreq++) {
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
                    int num_req_params=
                        ReqParams!=null? ReqParams[nreq]:
                        (fdt.FactoryImplObjectConstructor.GetParameters().Count(p => p.ParameterType==typeof(RunnerId))==0?1:2);
                    Assert.Equal(num_req_params, fdt._numberOfRequiredParams);
                    if(ExtraParams!=null) {
                        Assert.Equal(ExtraParams!.Length, fdt._extraArguments.Length);
                        for(int i = 0; i<ExtraParams!.Length; i++) {
                            Assert.IsType(ExtraParams![i].GetType(), fdt._extraArguments[i]);
                            Assert.True(ExtraParams![i].Equals(fdt._extraArguments[i]));
                        }

                    }
                    else
                        Assert.Empty(fdt._extraArguments);
                    Assert.Equal(typeof(TypeRunnerFactory<,>).MakeGenericType(type_args), fdt.FactoryImplObjectConstructor.DeclaringType);
                }
            }
        }

    }
}
