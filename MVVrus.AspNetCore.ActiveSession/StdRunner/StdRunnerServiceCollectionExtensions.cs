namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Contains extension methods used to configure standard runners from the ActiveSession library
    /// /// </summary>
    public static class StdRunnerServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method used to configure the adapter allowing use any sequence of <typeparamref name="TResult"/> objects as ActiveService runner
        /// </summary>
        /// <typeparam name="TResult">Type of objects in a sequence <see cref="IEnumerable{T}"/></typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
        /// <returns>Value of the Services param, used to facilitate call chaining</returns>
        /// <remarks>
        /// The adapter is created by <see cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)"/> call with
        /// the first type parameter to be of type <see cref="EnumAdapterParams{TRequest}"/> 
        /// and the second - of type <see cref="IEnumerable{TResult}"/>
        /// </remarks>
        public static IServiceCollection AddEnumAdapter<TResult>(this IServiceCollection Services)
        {
            return Services.AddActiveSessions<EnumAdapterRunner<TResult>>();
        }

        /// <summary>
        /// Extension method used to configure the adapter allowing use any sequence of <typeparamref name="TResult"/> objects as ActiveService runner
        /// </summary>
        /// <typeparam name="TResult">Type of objects in a sequence <see cref="IEnumerable{T}"/></typeparam>
        /// <param name="Services"><see cref="IServiceCollection"/> implementation to be used to configure an application service container</param>
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
        public static IServiceCollection AddEnumAdapter<TResult>(this IServiceCollection Services,
            Action<ActiveSessionOptions>? Configurator)
        {
            return Services.AddActiveSessions<EnumAdapterRunner<TResult>>(Configurator);
        }

    }
}
