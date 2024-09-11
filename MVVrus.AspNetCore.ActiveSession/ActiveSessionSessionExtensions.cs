namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO
    /// </summary>
    public static class ActiveSessionExtensions
    {
        /// <summary>
        /// A method used to create a new runner that uses an exlusively accessible service.
        /// </summary>
        /// <typeparam name="TRequest"><inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/typeparam[@name="TRequest"]'/></typeparam>
        /// <typeparam name="TResult"><inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">The <see cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)"/> instance for which the method to be perormed.</param>
        /// <param name="Request"><inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Request"]'/></param>
        /// <param name="Context"><inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]'/></param>
        /// <param name="ExclusiveServiceAccessor">
        /// The accessor for the exlusively accessible service used.
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
            KeyedRunner<TResult> result = Session.CreateRunner<TRequest, TResult>(Request, Context);
            (Session.TrackRunnerCleanup(result.RunnerNumber)??Task.CompletedTask).ContinueWith((_)=>ExclusiveServiceAccessor.Dispose());
            return result;
        }
    }
}
