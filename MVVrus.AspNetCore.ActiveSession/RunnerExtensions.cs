namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains extension methods for <see cref="IRunner{TResult}"/> interface
    /// </summary>
    /// <remarks>
    /// This class contains extension methods which allow transparent use of asynchronous methods 
    /// peculiar to <see cref="IRunnerProxy{TResult}"/> interface, having only a reference to an IRunner interface,
    /// irrespective to whether IRunnerProxy interface also implemented by an object implementing this IRunner interface or not.
    /// </remarks>
    public static class RunnerExtensions
    {
        /// <summary>
        /// <inheritdoc cref="IRunnerProxy.IsLocal" path="/summary"></inheritdoc>
        /// </summary>
        /// <param name="Runner">An instance of a runner for which the method is called.</param>
        /// <returns><see langword="true"/> if the runner is executed on the same host as a caller, <see langword="false"/> otherwise.</returns>
        public static Boolean IsLocal(this IRunner Runner)
        {
            return !(Runner is IRunnerProxy proxy) || proxy.IsLocal;
        }

        /// <summary>
        /// <univ1>A universal method of </univ1><uprop>obtaining a value of a property </uprop><see cref="IRunner.Status"/><univ2> for both local and remote runners.</univ2>
        /// </summary>
        /// <param name="Runner"><inheritdoc cref="IsLocal(IRunner)" path='/param[@name="Runner"]' /></param>
        /// <param name="Token"><inheritdoc cref="IRunnerProxy.GetStatusAsync(CancellationToken)" path='/param[@name="Token"]'/></param>
        /// <returns><inheritdoc cref="IRunnerProxy.GetStatusAsync(CancellationToken)" path="/returns"/></returns>
        public static ValueTask<RunnerStatus> GetStatusAsync(this IRunner Runner, CancellationToken Token = default)
        {
            if(Runner.IsLocal()) return new ValueTask<RunnerStatus>(Runner.Status);
            else return ((IRunnerProxy)Runner).GetStatusAsync(Token);
        }

        /// <summary>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ1"/>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/uprop"/>
        /// <see cref="IRunner.Position"/> 
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ2"/>
        /// </summary>
        /// <param name="Runner"><inheritdoc cref="IsLocal(IRunner)" path='/param[@name="Runner"]' /></param>
        /// <param name="Token"><inheritdoc cref="IRunnerProxy.GetPositionAsync(CancellationToken)" path='/param[@name="Token"]'/></param>
        /// <returns><inheritdoc cref="IRunnerProxy.GetPositionAsync(CancellationToken)" path="/returns"/></returns>
        public static ValueTask<Int32> GetPositionAsync(this IRunner Runner, CancellationToken Token = default)
        {
            if(Runner.IsLocal()) return new ValueTask<Int32>(Runner.Position);
            else return ((IRunnerProxy)Runner).GetPositionAsync(Token);
        }

        /// <summary>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ1"/>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/uprop"/>
        /// <see cref="IRunner.Exception"/> 
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ2"/>
        /// </summary>
        /// <param name="Runner"><inheritdoc cref="IsLocal(IRunner)" path='/param[@name="Runner"]' /></param>
        /// <param name="Token"><inheritdoc cref="IRunnerProxy.GetExceptionAsync(CancellationToken)" path='/param[@name="Token"]'/></param>
        /// <returns><inheritdoc cref="IRunnerProxy.GetExceptionAsync(CancellationToken)" path="/returns"/></returns>
        public static ValueTask<Exception?> GetExceptionAsync(this IRunner Runner, CancellationToken Token = default)
        {
            if(Runner.IsLocal()) return new ValueTask<Exception?>(Runner.Exception);
            else return ((IRunnerProxy)Runner).GetExceptionAsync(Token);
        }

        /// <summary>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ1"/>
        /// <ucall>calling a method </ucall>
        /// <see cref="IRunner.Abort(string?)"/> 
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ2"/>
        /// </summary>
        /// <param name="Runner"><inheritdoc cref="IsLocal(IRunner)" path='/param[@name="Runner"]' /></param>
        /// <param name="TraceIdentifier"><inheritdoc cref="IRunnerProxy.AbortAsync(string?, CancellationToken)" path='/param[@name="TraceIdentifier"]'/></param>
        /// <param name="Token"><inheritdoc cref="IRunnerProxy.AbortAsync(String?, CancellationToken)" path='/param[@name="Token"]'/></param>
        /// <returns><inheritdoc cref="IRunnerProxy.AbortAsync(string?, CancellationToken)" path="/returns"/></returns>
        public static Task AbortAsync(this IRunner Runner, String? TraceIdentifier = null, CancellationToken Token = default)
        {
            if(Runner.IsLocal()) { Runner.Abort(TraceIdentifier);  return Task.CompletedTask; }
            else return ((IRunnerProxy)Runner).AbortAsync(TraceIdentifier, Token);
        }

        /// <summary>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ1"/>
        /// <inheritdoc cref="AbortAsync(IRunner, string?, CancellationToken)" path="/summary/ucall"/>
        /// <see cref="IRunner.GetProgress"/> 
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ2"/>
        /// </summary>
        /// <param name="Runner"><inheritdoc cref="IsLocal(IRunner)" path='/param[@name="Runner"]' /></param>
        /// <param name="Token"><inheritdoc cref="IRunnerProxy.AbortAsync(String?, CancellationToken)" path='/param[@name="Token"]'/></param>
        /// <returns><inheritdoc cref="IRunnerProxy.GetProgressAsync(CancellationToken)" path="/returns"/></returns>
        public static ValueTask<RunnerBkgProgress> GetProgressAsync(this IRunner Runner, CancellationToken Token = default)
        {
            if(Runner.IsLocal()) return new ValueTask<RunnerBkgProgress>(Runner.GetProgress());
            else return ((IRunnerProxy)Runner).GetProgressAsync(Token);
        }

        /// <summary>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ1"/>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/uprop"/>
        /// <see cref="IRunner.IsBackgroundExecutionCompleted"/> 
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ2"/>
        /// </summary>
        /// <param name="Runner"><inheritdoc cref="IsLocal(IRunner)" path='/param[@name="Runner"]' /></param>
        /// <param name="Token"><inheritdoc cref="IRunnerProxy.GetExceptionAsync(CancellationToken)" path='/param[@name="Token"]'/></param>
        /// <returns><inheritdoc cref="IRunnerProxy.GetIsBackgroundExecutionCompletedAsync(CancellationToken)" path="/returns"/></returns>
        public static ValueTask<Boolean> GetIsBackgroundExecutionCompletedAsync(this IRunner Runner, CancellationToken Token = default)
        {
            if(Runner.IsLocal()) return new ValueTask<Boolean>(Runner.IsBackgroundExecutionCompleted);
            else return ((IRunnerProxy)Runner).GetIsBackgroundExecutionCompletedAsync(Token);
        }

        /// <summary>
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ1"/>
        /// <inheritdoc cref="AbortAsync(IRunner, string?, CancellationToken)" path="/summary/ucall"/>
        /// <see cref="IRunner{TResult}.GetAvailable(int, int, string?)"/> 
        /// <inheritdoc cref="GetStatusAsync(IRunner, CancellationToken)" path="/summary/univ2"/>
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="IRunner{TResult}" path="/typeparam"/></typeparam>
        /// <param name="Runner"><inheritdoc cref="IsLocal(IRunner)" path='/param[@name="Runner"]' /></param>
        /// <param name="Advance"><inheritdoc cref="IRunnerProxy{TResult}.GetAvailableAsync(int, int, string?, CancellationToken)" path='/param[@name="Advance"]'/></param>
        /// <param name="StartPosition"><inheritdoc cref="IRunnerProxy{TResult}.GetAvailableAsync(int, int, string?, CancellationToken)" path='/param[@name="StartPosition"]'/></param>
        /// <param name="TraceIdentifier"><inheritdoc cref="IRunnerProxy{TResult}.GetAvailableAsync(int, int, string?, CancellationToken)" path='/param[@name="TraceIdentifier"]'/></param>
        /// <param name="Token"><inheritdoc cref="IRunnerProxy.GetExceptionAsync(CancellationToken)" path='/param[@name="Token"]'/></param>
        /// <returns><inheritdoc cref="IRunnerProxy{TResult}.GetAvailableAsync(int, int, string?, CancellationToken)" path='/returns'/></returns>
        public static ValueTask<RunnerResult<TResult>> GetAvailableAsync<TResult>(this IRunner<TResult> Runner,
            Int32 Advance = IRunner.MAXIMUM_ADVANCE,
            Int32 StartPosition = IRunner.CURRENT_POSITION,
            String? TraceIdentifier = null,
            CancellationToken Token = default)
        {
            if(Runner.IsLocal()) return new ValueTask<RunnerResult<TResult>>(Runner.GetAvailable(Advance, StartPosition, TraceIdentifier));
            else return ((IRunnerProxy<TResult>)Runner).GetAvailableAsync(Advance, StartPosition, TraceIdentifier, Token);
        }

        /// <summary>
        /// A synchronous counterpart of <see cref="IRunner{TResult}.GetRequiredAsync(int, CancellationToken, int, string?)">IRunner&lt;TResult&gt;.GetRequiredAsync</see> method.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="IRunner{TResult}" path="/typeparam"/></typeparam>
        /// <param name="Runner">An instance of a runner for wich the method is called</param>
        /// <param name="Advance"><inheritdoc cref="IRunner{TResult}.GetRequiredAsync(int, CancellationToken, int, string?)" path='/param[@name="Advance"]' /></param>
        /// <param name="StartPosition"><inheritdoc cref="IRunner{TResult}.GetRequiredAsync(int, CancellationToken, int, string?)" path='/param[@name="StartPosition"]' /></param>
        /// <param name="TraceIdentifier"><inheritdoc cref="IRunner{TResult}.GetRequiredAsync(int, CancellationToken, int, string?)" path='/param[@name="TraceIdentifier"]' /></param>
        /// <returns><inheritdoc cref="IRunner{TResult}.GetRequiredAsync(int, CancellationToken, int, string?)" path='/returns' /></returns>

        public static RunnerResult<TResult> GetRequired<TResult>(this IRunner<TResult> Runner,
            Int32 Advance = IRunner.DEFAULT_ADVANCE,
            Int32 StartPosition = IRunner.CURRENT_POSITION,
            String? TraceIdentifier = null
        )
        {
            return Runner.GetRequiredAsync(Advance, default, StartPosition, TraceIdentifier).AsTask().GetAwaiter().GetResult();
        }


    }
}
