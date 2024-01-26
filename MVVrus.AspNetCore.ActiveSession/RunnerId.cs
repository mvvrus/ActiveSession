namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// A type be used by a runner identifier. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The runner identifier consists of two parts: <see cref="IActiveSession.Id"/> of the session to which the runner belongs 
    /// and  the number assigned to the runner within the session. The identifier is used mainly for tracing purposes 
    /// and can not alway be assigned or even exposed by the runner via <see cref="IRunner.Id"/> property.
    /// In theses cases value of the identifier returned by the property equals default(RunnerId)
    /// </para>
    /// <para>
    /// The sting represetation of an identifier (returned by its <see cref="ToString"/> method) yas the form 
    /// "{<see cref="SessionId"/>}:#{<see cref="RunnerNumber"/>}"
    /// </para>
    /// </remarks>
    public record struct RunnerId
    {
        /// <value>
        /// <see cref="IActiveSession.Id"/> of the session to which the runner belongs
        /// </value>
        public String SessionId { get; init; }
        /// <value>
        /// A number assigned to the runner within the session
        /// </value>
        public Int32 RunnerNumber { get; init; }

        /// <summary>
        /// Constructor that initializes RunnerId instance value
        /// </summary>
        /// <param name="SessionId"><see cref="IActiveSession.Id"/> of the session to which the runner belongs</param>
        /// <param name="RunnerNumber">A number assigned to the runner within the session</param>
        public RunnerId(String SessionId, Int32 RunnerNumber)
        {
            this.SessionId=SessionId;
            this.RunnerNumber=RunnerNumber;
        }

        /// <summary>
        /// Converts a tupple value to a <see cref="RunnerId"/> instance
        /// </summary>
        /// <param name="Value">Value of type ValueTuple{String,Int32} to convert</param>
        public static implicit operator RunnerId(ValueTuple<String, Int32> Value) 
        { 
            return new RunnerId(Value.Item1,Value.Item2);
        }

        ///<inheritdoc/>
        public override String ToString()
        {
            return this==default?$"{SessionId}:#{RunnerNumber}":"<Unknown RunnerId>";
        }
    }
}
