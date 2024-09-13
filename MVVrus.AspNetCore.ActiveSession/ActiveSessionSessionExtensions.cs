namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains commonly used extinsion methods for the <see cref="IActiveSession"/> interface.
    /// </summary>
    public static class ActiveSessionExtensions
    {
        /// <summary>
        /// Create a new runner that uses an exlusively accessible service.
        /// </summary>
        /// <typeparam name="TRequest"><inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/typeparam[@name="TRequest"]'/></typeparam>
        /// <typeparam name="TResult"><inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">The <see cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)"/> instance for which the method to be perormed.</param>
        /// <param name="Request"><inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Request"]'/></param>
        /// <param name="Context"><inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]'/></param>
        /// <param name="ExclusiveServiceAccessor">
        /// The accessor for the exlusively accessible service to be used by the runner.
        /// This accessor will be disposed after the runner completion and cleanup thus releasing the lock 
        /// represented by the accessor on the exclusively accessible scoped service from the active session's container.
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path="/returns"/>
        /// </returns>
        public static KeyedRunner<TResult> CreateRunnerWithExclusiveService<TRequest,TResult> (
            this IActiveSession Session,
            TRequest Request,
            HttpContext Context,
            IDisposable ExclusiveServiceAccessor)
        {
            return InternalCreateRunnerExcl<TRequest,TResult>(Session, Request, Context, ExclusiveServiceAccessor);
        }

        internal static KeyedRunner<TResult> InternalCreateRunnerExcl<TRequest, TResult>(
            IActiveSession Session,
            TRequest Request,
            HttpContext Context,
            IDisposable? ExclusiveServiceAccessor)
        {
            KeyedRunner<TResult> result = Session.CreateRunner<TRequest, TResult>(Request, Context);
            if(ExclusiveServiceAccessor!=null) {
                (Session.TrackRunnerCleanup(result.RunnerNumber)??Task.CompletedTask)
                    .ContinueWith((_) => ExclusiveServiceAccessor.Dispose(),TaskContinuationOptions.ExecuteSynchronously);
            }
            return result;
        }
    }
}
