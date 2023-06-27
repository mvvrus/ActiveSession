using static MVVrus.AspNetCore.ActiveSession.IActiveSessionRunner;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains extension methods for the IActiveSessionRunner interface
    /// </summary>
    public static class ActiveSessionRunnerExtensions
    {
        /// <summary>
        /// A simplified form of <see cref="IActiveSessionRunner{TResult}.GetMoreAsync(int, int, String?, CancellationToken)"></see> method always used the default value for an Advance parameter
        /// </summary>
        /// <param name="Runner">The runner instance to be used by this extension method </param>
        /// <param name="StartPosition">
        /// Position value from which the fetching of the results should begin
        /// <remarks> 
        /// <para>Use <see cref="IActiveSessionRunner.BEGINNING_POSITION"/> constant to fetch results from the very begining.</para>
        /// <para>Use <see cref="IActiveSessionRunner.CURRENT_POSITION"/> constant to continue to fetch results from the last position fetched.</para>
        /// </remarks>
        /// </param>
        /// <param name="Token"><see cref="CancellationToken"/> that may be used to cancel the method execution</param>
        /// <returns> 
        /// A task returning an <see cref="ActiveSessionRunnerResult{TResult}"/> value containing the state, the position of the runner at the point of completion 
        /// and the result (of type <typeparamref name="TResult"/>) if any
        /// </returns>
        public static ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync<TResult>(
            this IActiveSessionRunner<TResult> Runner,
            Int32 StartPosition, 
            CancellationToken Token = default
        )
        {
            return Runner.GetMoreAsync(StartPosition, DEFAULT_ADVANCE, null, Token);
        }

        //public static ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync<TResult>(
        //    this IActiveSessionRunner<TResult> Runner,
        //    Int32 StartPosition,
        //    CancellationToken Token = default
        //)
        //{
        //    return Runner.GetMoreAsync(StartPosition, DEFAULT_ADVANCE, null, Token);
        //}

    }
}
