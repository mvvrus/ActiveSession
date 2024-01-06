using static MVVrus.AspNetCore.ActiveSession.IRunner;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Contains extension methods for the IRunner interface
    /// </summary>
    public static class RunnerExtensions
    {
        /// <summary>
        /// A simplified form of <see cref="IRunner{TResult}.GetMoreAsync(int, int, String?, CancellationToken)"></see> method always used the default value for an Advance parameter
        /// </summary>
        /// <param name="Runner">The runner instance to be used by this extension method </param>
        /// <param name="StartPosition">
        /// Position value from which the fetching of the results should begin
        /// <remarks> 
        /// <para>Use <see cref="IRunner.BEGINNING_POSITION"/> constant to fetch results from the very begining.</para>
        /// <para>Use <see cref="IRunner.CURRENT_POSITION"/> constant to continue to fetch results from the last position fetched.</para>
        /// </remarks>
        /// </param>
        /// <param name="Token"><see cref="CancellationToken"/> that may be used to cancel the method execution</param>
        /// <returns> 
        /// A task returning an <see cref="RunnerResult{TResult}"/> value containing the state, the position of the runner at the point of completion 
        /// and the result (of type <typeparamref name="TResult"/>) if any
        /// </returns>
        public static ValueTask<RunnerResult<TResult>> GetMoreAsync<TResult>(
            this IRunner<TResult> Runner,
            Int32 StartPosition, 
            CancellationToken Token = default
        )
        {
            return Runner.GetMoreAsync(StartPosition, DEFAULT_ADVANCE, null, Token);
        }

        //public static ValueTask<RunnerResult<TResult>> GetMoreAsync<TResult>(
        //    this IRunner<TResult> Runner,
        //    Int32 StartPosition,
        //    CancellationToken Token = default
        //)
        //{
        //    return Runner.GetMoreAsync(StartPosition, DEFAULT_ADVANCE, null, Token);
        //}

    }
}
