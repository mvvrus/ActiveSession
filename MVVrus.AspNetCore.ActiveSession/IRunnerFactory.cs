﻿namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// A factory object used to create a new runner
    /// </summary>
    /// <typeparam name="TRequest">Type of the initialization data used to create a new runner</typeparam>
    /// <typeparam name="TResult">Type of the result, returned by the runner</typeparam>
    public interface IRunnerFactory<TRequest, TResult>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Request">The runner object initialization data.</param>
        /// <param name="Services">
        /// A <see cref="IServiceProvider"/> interface of a services container, which can be used 
        /// for dependency injection while creating the runner object.
        /// </param>
        /// <param name="RunnerId">Identifier (<see cref="RunnerId"/>) to be assigned to the runner created.</param>
        /// <param name="TraceIdentifier">The trace identifier of the request in which the method is called (for tracing purposes)</param>
        /// <returns>A new runner object implementing <see cref="IRunner{TResult}"/> interface or null if the object cannot be created</returns>
        IRunner<TResult>? Create(TRequest Request, IServiceProvider Services, RunnerId RunnerId, String? TraceIdentifier=null);
    }
}
