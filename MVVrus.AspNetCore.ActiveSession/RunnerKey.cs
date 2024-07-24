namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The type to be used by a runner identifier in a code external to an ActiveSession library
    /// </summary>
    /// <param name="RunnerNumber">A number assigned to the runner within the Active Session it belongs to.</param>
    /// <param name="Generation">Generation property value of the ActiveSession</param>
    /// <remarks>
    /// The string represetation of an identifier (returned by its <see cref="ToString"/> method) has the form 
    /// <br/>"{<see cref="Generation"/>-{<see cref="RunnerNumber"/>"
    /// </remarks>
    public record struct RunnerKey(Int32 RunnerNumber, Int32 Generation)
    {

        /// <summary>
        /// Converts a tuple of two integers to a RunnerKey value.
        /// </summary>
        /// <param name="Value">The tuple to convert.</param>
        public static implicit operator  RunnerKey((Int32 RunnerNumber, Int32 Generation) Value)
        {
            return new RunnerKey(Value.RunnerNumber, Value.Generation);
        }

        /// <summary>
        /// Returns a string representation of the value assigned to this instance.
        /// </summary>
        /// <returns>
        /// String "{<see cref="Generation"/>}-{<see cref="RunnerNumber"/>}" if the instance has a value
        /// or "&lt;Unknown RunnerId&gt;" if the instance is unassigned (has the default value).
        /// </returns>
        public override String ToString()
        {
            return $"{Generation}-{RunnerNumber}";
        }
    }
}
