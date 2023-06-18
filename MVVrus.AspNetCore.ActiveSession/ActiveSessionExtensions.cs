namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The class contains extension methods for the <see cref="IActiveSession"/> interface with more friendly names and signatures
    /// </summary>
    public static class ActiveSessionExtensions
    {
        /// <summary>
        /// A method to create a new runner (an object implementing <see cref="IActiveSessionRunner{TResult}"/> interface)
        /// </summary>
        /// <typeparam name="TRequest">Type of the initialization data used to create a runner</typeparam>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="ActiveSession">Active Session (implmentation of <see cref="IActiveSession"/> interface) in which the runner is created</param>
        /// <param name="Request">Initialization data (of type <typeparamref name="TRequest"/>)</param>
        /// <param name="RunnerNumber">Output parameter accepting the <see cref="Int32"/> key for the created runner </param>
        /// <returns>The created runner (of type <see cref="IActiveSessionRunner{TResult}"/>) </returns>
        public static IActiveSessionRunner<TResult> GetNewRunner<TRequest, TResult>(
            this IActiveSession ActiveSession, 
            TRequest Request,
            out Int32 RunnerNumber
            ) 
        {
            (IActiveSessionRunner<TResult> runner, RunnerNumber)=ActiveSession.CreateRunner<TRequest, TResult>(Request);
            return runner;
        }

        /// <summary>
        /// A method to get an existing runner
        /// </summary>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="ActiveSession">Active Session (implmentation of <see cref="IActiveSession"/> interface) in which the runner is created</param>
        /// <param name="RunnerNumber">An <see cref="Int32"/>  key specifying the runner to search for</param>
        /// <returns>The existing runner (of type <see cref="IActiveSessionRunner{TResult}"/>) if any or null</returns>
        public static IActiveSessionRunner<TResult>? GetExistingRunner<TResult> (this IActiveSession ActiveSession, Int32 RunnerNumber)
        {
            return ActiveSession.GetRunner<TResult>(RunnerNumber);
        }

        /// <summary>
        /// A method to get an existing runner or create a new one, if the specified runner does not exist
        /// </summary>
        /// <typeparam name="TRequest">Type of the initialization data used to create a new runner</typeparam>
        /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
        /// <param name="ActiveSession">Active Session (implmentation of <see cref="IActiveSession"/> interface) in which the runner is searched for or created</param>
        /// <param name="Request">Initialization data (of type <typeparamref name="TRequest"/>)</param>
        /// <param name="RunnerNumber">
        /// Input and output parameter. As an input parameter it specifies an <see cref="Int32"/> key of the runner to search for.
        /// As an output parameter it accepts the <see cref="Int32"/> key for the created runner 
        /// </param>
        /// <returns></returns>
        public static IActiveSessionRunner<TResult> GetAnyRunner<TRequest, TResult>(
            this IActiveSession ActiveSession,
            TRequest Request,
            ref Int32 RunnerNumber
            )
        {
            return ActiveSession.GetRunner<TResult>(RunnerNumber) ?? 
                ActiveSession.GetNewRunner<TRequest, TResult>(Request, out RunnerNumber);
        }

    }
}
