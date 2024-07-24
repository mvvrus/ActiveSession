namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// A type to be used by a runner identifier. It usually exposed by a runner via its <see cref="IRunner.Id"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The runner identifier consists of two parts: <see cref="IActiveSession.Id"/> of the active session 
    /// to which the runner belongs and  the number assigned to the runner within the session. 
    /// </para>
    /// <para>
    /// The identifier is used mainly for tracing purposes.
    /// It can be unaasigned or even not exposed at all by runners of some types. 
    /// In these cases value of the identifier returned by the property equals <see langword="default"/>
    /// </para>
    /// <para>
    /// The sting represetation of an identifier (returned by its <see cref="ToString"/> method) has the form 
    /// <br/>"{<see cref="SessionId"/>}:#{<see cref="RunnerNumber"/>}"
    /// </para>
    /// </remarks>
    public record struct RunnerId
    {
        //TODO Add Generation
        /// <summary>
        /// <see cref="IActiveSession.Id"/> of the active session to which the runner belongs.
        /// </summary>
        public String SessionId { get; init; }
        /// <summary>
        /// A number assigned to the runner within the session.
        /// </summary>
        public Int32 RunnerNumber { get; init; }

        /// <summary>
        /// Constructor that initializes RunnerId instance value.
        /// </summary>
        /// <param name="SessionId"><see cref="IActiveSession.Id"/> of the avtive session to which the runner belongs</param>
        /// <param name="RunnerNumber">A number assigned to the runner within the session</param>
        public RunnerId(String SessionId, Int32 RunnerNumber)
        {
            this.SessionId=SessionId;
            this.RunnerNumber=RunnerNumber;
        }

        /// <summary>
        /// Converts a tuple value to a <see cref="RunnerId"/> instance.
        /// </summary>
        /// <param name="Value">Value of type ValueTuple&lt;String,Int32&gt; to be converted.</param>
        public static implicit operator RunnerId(ValueTuple<String, Int32> Value) 
        { 
            return new RunnerId(Value.Item1,Value.Item2);
        }

        /// <summary>
        /// Returns a string representation of the value assigned to this instance.
        /// </summary>
        /// <returns>
        /// String "{<see cref="SessionId"/>}:#{<see cref="RunnerNumber"/>}" if the instance has a value
        /// or "&lt;Unknown RunnerId&gt;" if the instance is unassigned (has the default value).
        /// </returns>
        public override String ToString()
        {
            return this!=default?$"{SessionId}:#{RunnerNumber}":"<Unknown RunnerId>";
        }
    }
}
