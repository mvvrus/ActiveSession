#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do) 
namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Contains extension methods for <see cref="IServiceCollection"/> interface used to configure runner factories for standard runners defined in the ActiveSession library.
    /// </summary>
    public static class StdRunnerServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a runner factory to the application's DI container for <see cref="EnumAdapterRunner{TItem}"/>.
        /// </summary>
        /// <typeparam name="TItem">Type of objects in a sequence (<see cref="IEnumerable{T}">IEnumerable&lt;TItem&gt;</see>) for which the adapter to be created.</typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <returns>Value of the Services param, this value is used to facilitate call chaining.</returns>
        /// <remarks>
        /// <common>
        /// The factory created will be used for creation of a runner via following extension methods: 
        /// </common>
        /// <see cref="StdRunnerActiveSessionExtensions.CreateSequenceRunner{TItem}(IActiveSession, IEnumerable{TItem}, HttpContext, IDisposable)"/>
        /// or <see cref="StdRunnerActiveSessionExtensions.CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext, IDisposable)"/>
        /// </remarks>
        public static IServiceCollection AddEnumAdapter<TItem>(this IServiceCollection Services)
        {
            return Services.AddActiveSessions<EnumAdapterRunner<TItem>>();
        }

        /// <param name="Configurator">
        /// The delegate used to configure additional options (of type <see cref="ActiveSessionOptions"></see>) 
        /// for the ActiveSession library features. May be null, if no additional configuraion to be performed
        /// </param>
        /// <inheritdoc cref="AddEnumAdapter{TItem}(IServiceCollection)" />
        public static IServiceCollection AddEnumAdapter<TItem>(this IServiceCollection Services,
            Action<ActiveSessionOptions>? Configurator)
        {
            return Services.AddActiveSessions<EnumAdapterRunner<TItem>>(Configurator);
        }

        /// <summary>
        /// Adds a runner factory to the application's DI container for <see cref="AsyncEnumAdapterRunner{TItem}"/>.
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="AddEnumAdapter{TItem}(IServiceCollection)" path="/remarks/common/node()"/>
        /// <see cref="StdRunnerActiveSessionExtensions.CreateSequenceRunner{TItem}(IActiveSession, IAsyncEnumerable{TItem}, HttpContext, IDisposable?)"/>
        /// or <see cref="StdRunnerActiveSessionExtensions.CreateSequenceRunner{TItem}(IActiveSession, AsyncEnumAdapterParams{TItem}, HttpContext, IDisposable?)"/>
        /// </remarks>
        /// <inheritdoc cref="AddEnumAdapter{TItem}(IServiceCollection)" path="/*[not(self::summary)]"/>
        public static IServiceCollection AddAsyncEnumAdapter<TItem>(this IServiceCollection Services)
        {
            return Services.AddActiveSessions<AsyncEnumAdapterRunner<TItem>>();
        }

        /// <param name="Configurator"><inheritdoc cref="AddEnumAdapter{TItem}(IServiceCollection, Action{ActiveSessionOptions}?)" path='/param[@name="Configurator"]' /></param>
        /// <inheritdoc cref="AddAsyncEnumAdapter{TItem}(IServiceCollection)"/>
        public static IServiceCollection AddAsyncEnumAdapter<TItem>(this IServiceCollection Services,
            Action<ActiveSessionOptions>? Configurator)
        {
            return Services.AddActiveSessions<AsyncEnumAdapterRunner<TItem>>(Configurator);
        }

        /// <summary>
        /// Adds a runner factory to the application's DI container for <see cref="TimeSeriesRunner{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">A type of the value part of the tuples making up the returned time series</typeparam>
        /// <remarks>
        /// <inheritdoc cref="AddEnumAdapter{TResult}(IServiceCollection)" path="/remarks/common/node()"/>
        /// <see cref="StdRunnerActiveSessionExtensions.CreateTimeSeriesRunner{TResult}(IActiveSession, Func{TResult}, TimeSpan, HttpContext, IDisposable?)"/>
        /// , <see cref="StdRunnerActiveSessionExtensions.CreateTimeSeriesRunner{TResult}(IActiveSession, Func{TResult}, TimeSpan, int, HttpContext, IDisposable?)"/>
        /// or <see cref="StdRunnerActiveSessionExtensions.CreateTimeSeriesRunner{TResult}(IActiveSession, TimeSeriesParams{TResult}, HttpContext, IDisposable?)"/>
        /// </remarks>
        /// <inheritdoc cref="AddEnumAdapter{TResult}(IServiceCollection)" path="/*[not(self::summary)]"/>
        public static IServiceCollection AddTimeSeriesRunner<TResult>(this IServiceCollection Services)
        {
            return Services.AddActiveSessions<TimeSeriesRunner<TResult>>();
        }

        /// <param name="Configurator"><inheritdoc cref="AddEnumAdapter{TItem}(IServiceCollection, Action{ActiveSessionOptions}?)" path='/param[@name="Configurator"]' /></param>
        /// <inheritdoc cref="AddTimeSeriesRunner{TResult}(IServiceCollection)"/>
        public static IServiceCollection AddTimeSeriesRunner<TResult>(this IServiceCollection Services,
            Action<ActiveSessionOptions>? Configurator)
        {
            return Services.AddActiveSessions<TimeSeriesRunner<TResult>>(Configurator);
        }

        /// <summary>
        /// Adds a runner factory to the application's DI container for <see cref="SessionProcessRunner{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">Type of the result returned by the running process.</typeparam>
        /// <inheritdoc cref="AddEnumAdapter{TResult}(IServiceCollection)" path="/*[not(self::summary)]"/>
        public static IServiceCollection AddSessionProcessRunner<TResult>(this IServiceCollection Services)
        {
            return Services.AddActiveSessions<SessionProcessRunner<TResult>>();
        }

        /// <param name="Configurator"><inheritdoc cref="AddEnumAdapter{TItem}(IServiceCollection, Action{ActiveSessionOptions}?)" path='/param[@name="Configurator"]' /></param>
        /// <inheritdoc cref="AddSessionProcessRunner{TResult}(IServiceCollection)"/>
        public static IServiceCollection AddSessionProcessRunner<TResult>(this IServiceCollection Services,
            Action<ActiveSessionOptions>? Configurator)
        {
            return Services.AddActiveSessions<SessionProcessRunner<TResult>>(Configurator);
        }


#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    }
}
