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
        /// Type used to specialize a <see cref="IActiveSessionRunner"></see> generic interface, 
        /// returned by the factory delegate.
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="Factory">
        /// The factory delegate to be used by the runner factory service.
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remark>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IActiveSessionRunnerFactory{TRequest,TResult}"/>
        /// 
        /// In this overload the factory delegate does not use service container 
        /// and no configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remark>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IActiveSessionRunner<TResult>> Factory)
        {
            return AddActiveSessions(Services, Factory, null);
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
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
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remark>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IActiveSessionRunnerFactory{TRequest,TResult}">.</see>
        /// 
        /// In this overload the factory delegate does not use service container 
        /// and a configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remark>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
        Func<TRequest, IActiveSessionRunner<TResult>> Factory,
        Action<ActiveSessionOptions>? Configurator)
        {
            return AddActiveSessions<TRequest, TResult>(Services, (Request, _) => Factory(Request), Configurator);
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
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
        /// <remark>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IActiveSessionRunnerFactory{TRequest,TResult}">.</see>
        /// 
        /// In this overload the factory delegate does use service container 
        /// and no configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remark>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> Factory)
        {
            return AddActiveSessions(Services, Factory, null);
        }

        /// <summary>
        /// Extension method to configure use of an factory-based variant of runner factory service
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
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remarks>
        /// A runner factory service is a specialization of the 
        /// generic runner factory interface <see cref="IActiveSessionRunnerFactory{TRequest,TResult}">.</see>
        /// 
        /// In this overload the factory delegate does use service container 
        /// and a configuration delegate for changing <see cref="ActiveSessionOptions"></see> is used
        /// </remarks>
        public static IServiceCollection AddActiveSessions<TRequest, TResult>(this IServiceCollection Services,
            Func<TRequest, IServiceProvider, IActiveSessionRunner<TResult>> Factory, Action<ActiveSessionOptions>? Configurator)
        {
            AddActiveSessionInfrastructure(Services, Configurator);
            ActiveSessionStore.RegisterTResult(typeof(TResult));
            Services.TryAddSingleton<IActiveSessionRunnerFactory<TRequest, TResult>>
                    (sp => new DelegateRunnerFactory<TRequest, TResult>(Factory));
            return Services;
        }

        /// <summary>
        /// Extension method used to configure a type-based variant of runner factory services
        /// </summary>
        /// <typeparam name="TRunner">Class used as implementation of a runner
        /// implementing one or more specializations of <see cref="IActiveSessionRunner"></see> generic interface
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="ExtraArguments">Additional arguments to pass into TRunner constructor</param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// Runner factory service is a specializations of the generic runner factory interface <see cref="IActiveSessionRunnerFactory{TRequest,TResult}">.</see>
        /// Specializations of  <see cref="IActiveSessionRunnerFactory{TRequest,TResult}"></see>  
        /// for all combinations of TRequest and TResult supported by the TRunner type are added
        /// In this overload a configuration delegate for changing <see cref="ActiveSessionOptions"></see> is not used
        /// </remarks>
        public static IServiceCollection AddActiveSessions<TRunner>(this IServiceCollection Services, params Object[] ExtraArguments)
        {
            return AddActiveSessions<TRunner>(Services, null, ExtraArguments);
        }

        /// <summary>
        /// Extension method used to configure a type-based variant of runner factory services
        /// (specializations of the generic runner factory interface <see cref="IActiveSessionRunnerFactory{TRequest,TResult}">.</see>)
        /// Specializations of  <see cref="IActiveSessionRunnerFactory{TRequest,TResult}"></see>  
        /// for all combinations of TRequest and TResult supported by the TRunner type are added
        /// </summary>
        /// <typeparam name="TRunner">Class used as implementation of a runner
        /// implementing one or more specializations of <see cref="IActiveSessionRunner"></see> generic interface
        /// </typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <param name="ExtraArguments">Additional arguments to pass into TRunner constructor</param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// Runner factory service is a specializations of the generic runner factory interface <see cref="IActiveSessionRunnerFactory{TRequest,TResult}">.</see>
        /// Specializations of  <see cref="IActiveSessionRunnerFactory{TRequest,TResult}"></see>  
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

            //Select all types (TResult) for which IActiveSessionRunner<TResult> is implemented by the class
            Type[] result_types = runner_type.FindInterfaces(
                (m, _) => m.IsConstructedGenericType&&m.GetGenericTypeDefinition()==typeof(IActiveSessionRunner<>)
                , null
                ).Select(type => type.GenericTypeArguments[0]).ToArray();
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
                throw new InvalidOperationException($"No suitable constructors found for type {runner_type.FullName}");

            //Select unique types of the first parameter of all selected constructors as request types (TRequest)
            IEnumerable<Type> unique_first_param_types =
                selected_constructors.Select(c => c.GetParameters()[0].ParameterType).Distinct();

            //Add to service list (Services) implementations of IActiveSessionRunnerFactory<TRequest,TResult>
            // via the class TypeRunnerFactory<TRequest,TResult>
            // for all combinations of request and result types 
            //            int extra_params_length = (ExtraArguments?.Length??0);
            Object[] factory_impl_params = new Object[3];
            Type[] type_args = new Type[2];
            foreach (Type result_type in result_types) {
                ActiveSessionStore.RegisterTResult(result_type);
                foreach (Type request_type in unique_first_param_types) {
                    type_args[0]=request_type;
                    type_args[1]=result_type;
                    Type factory_service_type = typeof(IActiveSessionRunnerFactory<,>)
                        .MakeGenericType(type_args);
                    ConstructorInfo factory_impl_object_constructor = typeof(TypeRunnerFactory<,>)
                        .MakeGenericType(type_args)
                        .GetConstructors()[0];

                    Services.AddSingleton(
                        factory_service_type, 
                        (new FactoryDelegateTarget(
                            factory_impl_object_constructor, 
                            runner_type, 
                            ExtraArguments
                        )).Invoke
                    );

                }

            }
            return Services;
        }

        /// <summary>
        /// Extension method used to configure the adapter allowing use any sequence of <typeparamref name="TRunner"/> objects as ActiveService runner
        /// </summary>
        /// <typeparam name="TRunner">Type of objects in a sequence <see cref="IEnumerable{T}"/></typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remarks>
        /// The adapter is created by <see cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)"/> call with
        /// the first type parameter to be of type <see cref="EnumAdapterParams{TRequest}"/> 
        /// and the second - of type <see cref="IEnumerable{TResult}"/>
        /// </remarks>
        public static IServiceCollection AddActiveSessionsEnumAdapter<TRunner>(this IServiceCollection Services)
        {
            return Services.AddActiveSessions<EnumAdapterRunner<TRunner>>();

        }

        /// <summary>
        /// Extension method used to configure the adapter allowing use any sequence of <typeparamref name="TRunner"/> objects as ActiveService runner
        /// </summary>
        /// <typeparam name="TRunner">Type of objects in a sequence <see cref="IEnumerable{T}"/></typeparam>
        /// <param name="Services">IServiceCollection implementation to be used to configure an application service container</param>
        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) for the ActiveSession feature
        /// May be null, if no additional configuraion to be performed
        /// </param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remarks>
        /// The adapter is created by <see cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)"/> call with
        /// the first type parameter to be of type <see cref="EnumAdapterParams{TRequest}"/> 
        /// and the second - of type <see cref="IEnumerable{TResult}"/>
        /// </remarks>
        public static IServiceCollection AddActiveSessionsEnumAdapter<TRunner>(this IServiceCollection Services,
            Action<ActiveSessionOptions>? Configurator)
        {
            return Services.AddActiveSessions<EnumAdapterRunner<TRunner>>(Configurator);
        }

        internal static void AddActiveSessionInfrastructure(IServiceCollection Services, Action<ActiveSessionOptions>? PostConfigurator)
        //The internal access modifier is for testing
        {
            //Add common services for the active sessions feature
            if (!Services.Any(s => s.ServiceType==typeof(IActiveSessionStore))) { //The first run of this method
                Services.TryAddSingleton<IActiveSessionStore, ActiveSessionStore>();
                Services.TryAddSingleton<IRunnerManagerFactory, RunnerManagerFactory>();
                Services.AddOptions<ActiveSessionOptions>().Configure<IConfiguration>(ReadActiveSessionsConfig);
            }
            if (PostConfigurator!=null)
                Services.AddOptions<ActiveSessionOptions>().PostConfigure(PostConfigurator);
            Services.AddHttpContextAccessor();
            Services.TryAddScoped<ActiveSessionServiceProviderRef>();
            Services.TryAdd(ServiceDescriptor.Scoped(typeof(IActiveSessionService<>),typeof(ActiveSessionService<>)));
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
            internal Object[] ExtraArguments;

            public FactoryDelegateTarget(
                ConstructorInfo FactoryImplObjectConstructor, 
                Type RunnerResultType, 
                Object[]? ExtraArguments
            ){
                this.FactoryImplObjectConstructor=FactoryImplObjectConstructor;
                this.RunnerResultType=RunnerResultType;
                this.ExtraArguments=ExtraArguments??new Object[0];
            }

            public Object Invoke(IServiceProvider sp)
            {

                return FactoryImplObjectConstructor.Invoke(new Object?[] {
                                RunnerResultType,
                                ExtraArguments,
                                sp.GetService<ILoggerFactory>() 
                            }
                );
            }
        }
    }
}
