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
    /// The string represetation of an identifier (returned by its <see cref="ToString"/> method) has the form 
    /// <br/>"{<see cref="SessionId"/>}:#{<see cref="Generation"/>-{<see cref="RunnerNumber"/>}"
    /// </para>
    /// </remarks>
    public record struct RunnerId
    {

        /// <summary>
        /// <see cref="IActiveSession.Id"/> of the active session to which the runner belongs.
        /// </summary>
        public String SessionId { get; init; }
        /// <summary>
        /// A number assigned to the runner within the session.
        /// </summary>
        public Int32 RunnerNumber { get; init; }
        /// <summary>
        /// An <see cref="IActiveSession.Generation"/> value to which the runner belongs.
        /// </summary>
        public Int32  Generation { get; init; }

        /// <summary>
        /// Constructor that initializes RunnerId instance value.
        /// </summary>
        /// <param name="SessionId"><see cref="IActiveSession.Id"/> of the active session to which the runner belongs.</param>
        /// <param name="RunnerNumber">A number assigned to the runner within the session.</param>
        /// <param name="Generation">An <see cref="IActiveSession.Generation"/> value of the active session to which the runner belongs.</param>
        public RunnerId(String SessionId, Int32 RunnerNumber, Int32 Generation)
        {
            this.SessionId=SessionId;
            this.RunnerNumber=RunnerNumber;
            this.Generation=Generation;
        }

        /// <summary>
        /// <inheritdoc cref="RunnerId.RunnerId(string, int, int)" path="/summary" />
        /// </summary>
        /// <param name="Session"><see cref="IActiveSession">Active session</see> to which the runner belongs.</param>
        /// <param name="RunnerNumber">
        /// <inheritdoc cref="RunnerId.RunnerId(string, int, int)" path='/param[@name="RunnerNumber"]' />
        /// </param>
        public RunnerId(IActiveSession Session, Int32 RunnerNumber):
            this(Session.Id, RunnerNumber, Session.Generation) { }

        /// <summary>
        /// Converts a tuple value to a <see cref="RunnerId"/> instance.
        /// </summary>
        /// <param name="Value">Value of type ValueTuple&lt;String,Int32&gt; to be converted.</param>
        public static implicit operator RunnerId((String SessionId, Int32 RunnerNumber, Int32 Generation) Value) 
        { 
            return new RunnerId(Value.SessionId,Value.RunnerNumber,Value.Generation);
        }

        /// <summary>
        /// Returns a string representation of the value assigned to this instance.
        /// </summary>
        /// <returns>
        /// String "{<see cref="SessionId"/>}:{<see cref="RunnerNumber"/>}#{<see cref="Generation"/>" if the instance has a value
        /// or "&lt;Unknown RunnerId&gt;" if the instance is unassigned (has the default value).
        /// </returns>
        public override String ToString()
        {
            return this!=default?$"{SessionId}:{Generation}#{RunnerNumber}":"<Unknown RunnerId>";
        }
    }
}
