using Microsoft.Extensions.DependencyInjection.Extensions;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Reflection;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains extension methods used to configure services for ActiveSession feature
    /// </summary>
    public static class ActiveSessionServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service.
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remark>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}"/>
        /// 
        /// In this overload the factory delegate does not use service container and set value for runner <see cref="IRunner.Id"/> property
        /// and no configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remark>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IRunner<TResult>> Factory)
        {
            return AddActiveSessions(Services, Factory, null);
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remark>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}">.</see>
        /// 
        /// In this overload the factory delegate does not use service container and set value for runner <see cref="IRunner.Id"/> property
        /// and a configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remark>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
        Func<TRequest, IRunner<TResult>> Factory,
        Action<ActiveSessionOptions>? Configurator)
        {
            return AddActiveSessions<TRequest, TResult>(Services, (Request, _, _) => Factory(Request), Configurator);
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remark>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}">.</see>
        /// 
        /// In this overload the factory delegate does use service container but not set value for runner <see cref="IRunner.Id"/> property
        /// and no configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remark>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, IRunner<TResult>> Factory)
        {
            return AddActiveSessions(Services, Factory, null);
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remarks>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}">.</see>
        /// 
        /// In this overload the factory delegate does use service container but not set value for runner <see cref="IRunner.Id"/> property 
        /// and a configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remarks>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, IRunner<TResult>> Factory, Action<ActiveSessionOptions>? Configurator)
        {
            return AddActiveSessions<TRequest, TResult>(Services, (Request, SP, _) => Factory(Request,SP), Configurator);
        }
        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remark>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}">.</see>
        /// 
        /// In this overload the factory delegate does use service container and set value for runner <see cref="IRunner.Id"/> property
        /// and no configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remark>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, RunnerId, IRunner<TResult>> Factory)
        {
            return AddActiveSessions(Services, Factory, null);
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
        /// </summary>
        /// <typeparam name="TRequest">Type of the input parameter of factory delegate.</typeparam>
        /// <typeparam name="TResult">
        /// Type used to specialize a <see cref="IRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remarks>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}">.</see>
        /// 
        /// In this overload the factory delegate does use service container and set value for runner <see cref="IRunner.Id"/> property
        /// and a configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remarks>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, RunnerId, IRunner<TResult>> Factory, Action<ActiveSessionOptions>? Configurator)
        {
            AddActiveSessionInfrastructure(Services, Configurator);
            ActiveSessionStore.RegisterTResult(typeof(TResult));
            Services.TryAddSingleton<IRunnerFactory<TRequest, TResult>>
                    (sp => new DelegateRunnerFactory<TRequest, TResult>(Factory));
            return Services;
        }

        /// <summary>
        /// Extension method used to configure a type-based variant of runner factory services
        /// </summary>
        /// <typeparam name="TRunner">Class used as implementation of a runner
        /// implementing one or more specializations of <see cref="IRunner"></see> generic interface
        /// </typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="ExtraArguments">Additional arguments to pass into TRunner constructor</param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// Runner factory service is a specializations of the generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}">.</see>
        /// Specializations of  <see cref="IRunnerFactory{TRequest,TResult}"></see>  
        /// for all combinations of TRequest and TResult supported by the TRunner type are added
        /// In this overload a configuration delegate for changing <see cref="ActiveSessionOptions"></see> is not used
        /// </remarks>
        public static IServiceCollection AddActiveSessions<TRunner>(this IServiceCollection Services, params Object[] ExtraArguments)
        {
            return AddActiveSessions<TRunner>(Services, null, ExtraArguments);
        }

        /// <summary>
        /// Extension method used to configure a type-based variant of runner factory services
        /// (specializations of the generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}">.</see>)
        /// Specializations of  <see cref="IRunnerFactory{TRequest,TResult}"></see>  
        /// for all combinations of TRequest and TResult supported by the TRunner type are added
        /// </summary>
        /// <typeparam name="TRunner">Class used as implementation of a runner
        /// implementing one or more specializations of <see cref="IRunner"></see> generic interface
        /// </typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <param name="ExtraArguments">Additional arguments to pass into TRunner constructor</param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// Runner factory service is a specializations of the generic runner factory interface <see cref="IRunnerFactory{TRequest,TResult}">.</see>
        /// Specializations of  <see cref="IRunnerFactory{TRequest,TResult}"></see>  
        /// for all combinations of TRequest and TResult supported by the TRunner type are added
        /// In this overload a configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remarks>
        public static IServiceCollection AddActiveSessions<TRunner>(this IServiceCollection Services,
            Action<ActiveSessionOptions>? Configurator, params Object[] ExtraArguments)
        {
            AddActiveSessionInfrastructure(Services, Configurator);
            Type runner_type = typeof(TRunner);
            if (!runner_type.IsClass||runner_type.IsAbstract)
                throw new InvalidOperationException($"{runner_type.FullName} is not a non-abstract class.");

            //Select all types (TResult) for which IRunner<TResult> is implemented by the class
            Type[] result_types = runner_type.FindInterfaces(
                (m, _) => m.IsConstructedGenericType&&m.GetGenericTypeDefinition()==typeof(IRunner<>)
                , null
                ).Select(type => type.GenericTypeArguments[0]).ToArray();
            if (result_types.Length<=0)
                throw new InvalidOperationException(
                    $"{runner_type.FullName} does not implement any specialisation of IRunner<> generic interface"
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
                        if (has_params)
                            selected_constructors.Add(constructor);
                    }
                    else
                        continue;
                }
                else if (!has_marked_constructor&&has_params)
                    selected_constructors.Add(constructor);
            }

            Int32 count = selected_constructors.Count();
            if (count<=0)
                throw new InvalidOperationException($"No suitable constructors found for type {runner_type.FullName}");
            //Check uniqueness of the first parameter type for all selected constructors
            if (count!=selected_constructors.Select(c => c.GetParameters()[0].ParameterType).Distinct().Count())
                throw new InvalidOperationException($"Ambiguous constructors found for type {runner_type.FullName}");

            //Add to service list (Services) implementations of IRunnerFactory<TRequest,TResult>
            // via the class TypeRunnerFactory<TRequest,TResult>
            // for all combinations of request and result types 
            //            int extra_params_length = (ExtraArguments?.Length??0);
            Object[] factory_impl_params = new Object[3];
            Type[] type_args = new Type[2];
            foreach (Type result_type in result_types) {
                ActiveSessionStore.RegisterTResult(result_type);
                foreach (ConstructorInfo constructor in selected_constructors) {
                    Type request_type = constructor.GetParameters()[0].ParameterType;
                    type_args[0]=request_type;
                    type_args[1]=result_type;
                    Type factory_service_type = typeof(IRunnerFactory<,>)
                        .MakeGenericType(type_args);
                    ConstructorInfo factory_impl_object_constructor = typeof(TypeRunnerFactory<,>)
                        .MakeGenericType(type_args)
                        .GetConstructors().First();
                    Int32 num_req_param = 1;    
                    //Account for RunnerId constructor parameter
                    if (constructor.GetParameters().Count(p => p.ParameterType==typeof(RunnerId))==1) num_req_param++;
                    Services.AddSingleton(
                        factory_service_type, 
                        (new FactoryDelegateTarget(
                            factory_impl_object_constructor, 
                            runner_type,
                            num_req_param,
                            ExtraArguments
                        )).Invoke
                    );

                }

            }
            return Services;
        }

        /// <summary>
        /// Add infrastructure services for the ActiveSession fearure and optionally configure the feature
        /// </summary>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        public static IServiceCollection AddActiveSessionInfrastructure(this IServiceCollection Services, Action<ActiveSessionOptions>? Configurator=null)
        {
            if (!Services.Any(s => s.ServiceType==typeof(IActiveSessionStore))) { //The first run of this method
                Services.TryAddSingleton<IActiveSessionStore, ActiveSessionStore>();
                Services.TryAddSingleton<IRunnerManagerFactory, RunnerManagerFactory>();
                Services.TryAddSingleton<IActiveSessionIdSupplier, ActiveSessionIdSupplier>();
                Services.AddOptions<ActiveSessionOptions>().Configure<IConfiguration>(ReadActiveSessionsConfig);
            }
            if (Configurator!=null)
                Services.AddOptions<ActiveSessionOptions>().PostConfigure(Configurator);
            Services.AddHttpContextAccessor();
            Services.TryAddScoped<ActiveSessionServiceProviderRef>();
            Services.TryAdd(ServiceDescriptor.Scoped(typeof(IActiveSessionService<>),typeof(ActiveSessionService<>)));
            return Services;
        }

        private static void ReadActiveSessionsConfig(ActiveSessionOptions Options, IConfiguration Configuration)
        {
            Configuration.Bind(CONFIG_SECTION_NAME, Options);
        }

        //The class, containing the method to be called as factory creating TypeRunnerFactory specialization
        //It is used just to facilitate testing
        internal class FactoryDelegateTarget
        {
            internal ConstructorInfo FactoryImplObjectConstructor { get; init; }
            internal Type RunnerResultType { get; init; }
            internal Object[] _extraArguments;
            internal Int32 _numberOfRequiredParams=1;

            public FactoryDelegateTarget(
                ConstructorInfo FactoryImplObjectConstructor, 
                Type RunnerResultType,
                Int32 NumberOfRequiredParams,
                Object[]? ExtraArguments
            ){
                this.FactoryImplObjectConstructor=FactoryImplObjectConstructor;
                this.RunnerResultType=RunnerResultType;
                this._numberOfRequiredParams=NumberOfRequiredParams;
                this._extraArguments=ExtraArguments??new Object[0];
            }

            public Object Invoke(IServiceProvider sp)
            {

                return FactoryImplObjectConstructor.Invoke(new Object?[] {
                                RunnerResultType,
                                _extraArguments,
                                _numberOfRequiredParams,
                                sp.GetService<ILoggerFactory>() 
                            }
                );
            }
        }
    }
}
