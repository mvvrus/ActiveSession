﻿using Microsoft.Extensions.DependencyInjection.Extensions;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Reflection;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The ActiveSessionServiceCollectionExtensions class contains extension methods used to 
    /// configure services for ActiveSession feature
    /// </summary>
    public static class ActiveSessionServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// (specialization of the generic runner factory interface <see cref="IActiveSessionRunnerFactory">.</see>)
        /// In this overload the factory delegate does not use service container
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IActiveSessionRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IActiveSessionRunner<TResult>> Factory)
        {
            return AddActiveSessions(Services, Factory, o => { });
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// (specialization of the generic runner factory interface <see cref="IActiveSessionRunnerFactory">.</see>)
        /// In this overload  the factory delegate does not use service container
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IActiveSessionRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
        Func<TRequest, IActiveSessionRunner<TResult>> Factory,
        Action<ActiveSessionOptions> Configurator)
        {
            return AddActiveSessions<TRequest,TResult>(Services, (Request, _) => Factory(Request), Configurator);
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// (specialization of the generic runner factory interface <see cref="IActiveSessionRunnerFactory">.</see>)
        /// In this overload  the factory delegate does use service container
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IActiveSessionRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> Factory)
        {
            return AddActiveSessions(Services, Factory, o => { } );
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// (specialization of the generic runner factory interface <see cref="IActiveSessionRunnerFactory">.</see>)
        /// In this overload  the factory delegate does use service container
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IActiveSessionRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> Factory, Action<ActiveSessionOptions> Configurator)
        {
            AddActiveSessionsInfrastructure(Services, Configurator);
            ActiveSessionStore.RegisterTResult(typeof(TResult));
            Services.TryAddSingleton<IActiveSessionRunnerFactory<TRequest, TResult>>
                    (sp => new DelegateRunnerFactory<TRequest, TResult>(Factory));
            return Services;
        }

        /// <summary>
        /// Extension method to configure use of an type-based variant of runner factory services
        /// (specializations of the generic runner factory interface <see cref="IActiveSessionRunnerFactory">.</see>)
        /// Specializations of  <see cref="IActiveSessionRunnerFactory"></see>  
        /// for all combinations of TRequest and TResult supported by the TRunner type are added
        /// </summary>
        /// <typeparam name="TRunner">Class used as implementation of a runner
        /// implementing one or more specializations of <see cref="IActiveSessionRunner"></see> generic interface
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="ExtraArguments">Additional arguments to pass into TRunner constructor</param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IServiceCollection AddActiveSessions<TRunner>(this IServiceCollection Services, params Object[] ExtraArguments)
        {
            return AddActiveSessions<TRunner>(Services, o => { }, ExtraArguments);
        }

        /// <summary>
        /// Extension method to configure use of an type-based variant of runner factory services
        /// (specializations of the generic runner factory interface <see cref="IActiveSessionRunnerFactory">.</see>)
        /// Specializations of  <see cref="IActiveSessionRunnerFactory"></see>  
        /// for all combinations of TRequest and TResult supported by the TRunner type are added
        /// </summary>
        /// <typeparam name="TRunner">Class used as implementation of a runner
        /// implementing one or more specializations of <see cref="IActiveSessionRunner"></see> generic interface
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// </param>
        /// <param name="ExtraArguments">Additional arguments to pass into TRunner constructor</param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IServiceCollection AddActiveSessions<TRunner>(this IServiceCollection Services, 
            Action<ActiveSessionOptions> Configurator, params Object[] ExtraArguments)
        {
            AddActiveSessionsInfrastructure(Services, o => { });
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
                    if (has_params) selected_constructors.Add(constructor);
                    break;
                }
                ActiveSessionConstructorAttribute? as_attribute = constructor.GetCustomAttribute<ActiveSessionConstructorAttribute>();
                if (as_attribute!=null) {
                    if (as_attribute.Use) {
                        if (!has_marked_constructor) selected_constructors.Clear();
                        has_marked_constructor=true;
                        if (has_params) selected_constructors.Add(constructor);
                    }
                    else
                        continue;
                }
                else if (!has_marked_constructor&&has_params) selected_constructors.Add(constructor);
            }
            if (selected_constructors.Count<=0)
                throw new InvalidOperationException($"No suitable constructors found for type {runner_type.FullName}");

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

        private static void AddActiveSessionsInfrastructure(IServiceCollection Services, Action<ActiveSessionOptions>? PostConfigurator)
        {
            //Add common services for the active sessions feature
            if(!Services.Any(s => s.ServiceType==typeof(IActiveSessionStore))) { //The first run of this method
                Services.TryAddSingleton<IActiveSessionStore, ActiveSessionStore>();
                Services.AddOptions<ActiveSessionOptions>().Configure<IConfiguration>(ReadActiveSessionsConfig);
            }
            if (PostConfigurator!=null)
                Services.AddOptions<ActiveSessionOptions>().PostConfigure(PostConfigurator);
        }

        private static void ReadActiveSessionsConfig(ActiveSessionOptions Options, IConfiguration Configuration)
        {
            Configuration.Bind(CONFIG_KEY_NAME, Options);
        }
    }
}
