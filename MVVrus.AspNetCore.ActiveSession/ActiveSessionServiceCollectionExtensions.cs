using Microsoft.Extensions.DependencyInjection.Extensions;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Reflection;

namespace MVVrus.AspNetCore.ActiveSession
{
    public static class ActiveSessionServiceCollectionExtensions
    {
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IActiveSessionRunner<TResult>> Factory)
        {
            AddActiveSessionsInfrastructure(Services);
            return Services.AddSingleton<IActiveSessionRunnerFactory<TRequest, TResult>>(
                _ => new DelegateRunnerFactory<TRequest, TResult>(
                    (Request, _) => Factory(Request)
                )
            );
        }

        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> Factory)
        {
            AddActiveSessionsInfrastructure(Services);
            ActiveSessionStore.RegisterTResult(typeof(TResult));
            return Services.AddSingleton<IActiveSessionRunnerFactory<TRequest, TResult>>(
                sp => new DelegateRunnerFactory<TRequest, TResult>(Factory)
                );
        }

        public static IServiceCollection AddActiveSessions<TRunner>(this IServiceCollection Services, params Object[] ExtraArguments)
        {
            AddActiveSessionsInfrastructure(Services);
            Type runner_type = typeof(TRunner);
            if (!runner_type.IsClass||runner_type.IsAbstract)
                throw new InvalidOperationException($"{runner_type.FullName} is not a non-abstract class.");

            //Select all types (TResult) for which IActiveSessionRunner<TResult> is implemented by the class
            Type[] result_types = runner_type.FindInterfaces(
                (m, _) => m.IsConstructedGenericType&&m.GetGenericTypeDefinition()==typeof(IActiveSessionRunner<>)
                , null
                );
            if (result_types.Length<=0)
                throw new InvalidOperationException(
                    $"{runner_type.FullName} does not implement any specialisation of IActiveSessionRunner<> generic interface"
                    );

            //Find suitable constructors: 
            //  Constructor selection rules
            //  1. If any constructor has ActivatorUtilitiesConstructorAttribute - select it and only it
            //     because no other constructors can be used by TypeRunnerFactory implementation
            //     because it uses ActivatorUtilities.CreateInstance to construct the type instance
            //  2. If any constructors are marked by ActiveSessionConstructorAttribute(true) - select all such constructors
            //  3. If no constructors are marked by ActiveSessionConstructorAttribute(true) - - select all such constructors
            //     with at least one parameter except those marked by ActiveSessionConstructorAttribute(true)
            List<ConstructorInfo> selected_constructors = new();
            Boolean has_marked_constructor = false;
            foreach (ConstructorInfo? constructor in runner_type.GetConstructors()) {
                Boolean has_params = constructor.GetParameters().Length>0;
                if (constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false)) {
                    selected_constructors.Clear();
                    if (has_params)
                        selected_constructors.Add(constructor);
                    break;
                }
                ActiveSessionConstructorAttribute? as_attribute = constructor.GetCustomAttribute<ActiveSessionConstructorAttribute>();
                if (as_attribute!=null) {
                    if (as_attribute.Use) {
                        if (!has_marked_constructor)
                            selected_constructors.Clear();
                        has_marked_constructor=true;
                        //TODO Check the constructor to have at least one parameter
                        if (has_params)
                            selected_constructors.Add(constructor);
                    }
                    else
                        continue;
                }
                else if (!has_marked_constructor&&has_params)
                    selected_constructors.Add(constructor);
            }
            if (selected_constructors.Count<=0)
                throw new InvalidOperationException($"No suitable constructors found for type ");

            //Select unique types of the first parameter of all selected constructors as request types (TRequest)
            IEnumerable<Type> unique_first_param_types =
                selected_constructors.Select(c => c.GetParameters()[0].ParameterType).Distinct();

            //Add to service list (Services) implementations of IActiveSessionRunnerFactory<TRequest,TResult>
            // via the class TypeRunnerFactory<TRequest,TResult>
            // for all combinations of request and result types 
            foreach (Type result_type in result_types) {
                ActiveSessionStore.RegisterTResult(result_type);
                foreach (Type request_type in unique_first_param_types) {
                    Type[] type_args = new Type[] { request_type, result_type };
                    Type factory_service_type = typeof(IActiveSessionRunnerFactory<,>)
                        .MakeGenericType(type_args);
                    int extra_params_length = (ExtraArguments?.Length??0);
                    Object[] factory_impl_params = new Object[extra_params_length+1];
                    if (extra_params_length>0)
                        Array.Copy(ExtraArguments!, 0, factory_impl_params, 1, extra_params_length);
                    factory_impl_params[0]=runner_type;
                    Object factory_impl_object = typeof(TypeRunnerFactory<,>)
                        .MakeGenericType(type_args)
                        .GetConstructors()[0]
                        .Invoke(factory_impl_params);
                    Services.AddSingleton(factory_service_type, factory_impl_object);

                }

            }
            return Services;
        }

        private static void AddActiveSessionsInfrastructure(IServiceCollection Services)
        {
            //Add common services for the active sessions feature
            Services.TryAddSingleton<IActiveSessionStore, ActiveSessionStore>();
            //TODO
            //Find a way to pass configuration values
            Services.AddOptions<ActiveSessionOptions>().Configure<IConfiguration>(ReadActiveSessionsConfig);
        }

        private static void ReadActiveSessionsConfig(ActiveSessionOptions Options, IConfiguration Configuration)
        {
            Configuration.Bind(ActiveSessionOptions.CONFIG_KEY_NAME, Options);
        }
    }
}
