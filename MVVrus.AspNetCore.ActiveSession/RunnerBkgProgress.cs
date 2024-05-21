namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Used to return information about a progress of a background execution of the runner
    /// </summary>
    /// <param name="Progress">A <see cref="IRunner.Position"/> value wich a background execution have been reached</param>
    /// <param name="EstimatedEnd">An estimation of a <see cref="IRunner.Position"/> value to be at a finish of the background execution(if any)</param>
    /// <remarks>The progress infotmation mentioned is returned via <see cref="IRunner.GetProgress"/> method</remarks>
    public record struct RunnerBkgProgress(Int32 Progress, Int32? EstimatedEnd)
    {
        /// <summary>
        /// Convert a tuple of values with appropriate types to an instance of this struct.
        /// </summary>
        /// <param name="Value">The value to be converted.</param>
        public static implicit operator RunnerBkgProgress(ValueTuple<Int32, Int32?> Value)
        {
            return new RunnerBkgProgress(Value.Item1, Value.Item2);
        }
    }
}
