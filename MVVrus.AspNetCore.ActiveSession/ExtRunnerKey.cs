using System.Net;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This struct is intended to pass a runner identifier into a code external to an ActiveSession library (e.g as a part of URL).
    /// </summary>
    /// <param name="RunnerNumber">A number assigned to the runner within the Active Session it belongs to.</param>
    /// <param name="ActiveSessionId">
    /// An identifier (<see cref="IActiveSession.Id">Id</see> property value) of the Active Session the runner belongs to.
    /// </param>
    /// <param name="Generation">
    /// The <see cref="IActiveSession.Generation">Generation</see> property value of the ActiveSession the runner belongs to.
    /// </param>
    /// <remarks>
    /// A purpose of this type is to avoid a mess if an ActiveSession assigned to different HTTP requests differs.
    ///<br/> The string represetation of an identifier (returned by its <see cref="ToString"/> method
    /// and parseable by its static <see cref="TryParse(string, out ExtRunnerKey)">TryParse</see> method) has the form 
    /// <br/><inheritdoc cref="ToString" path='/returns/format' />".
    /// </remarks>
    public record struct ExtRunnerKey(Int32 RunnerNumber, String ActiveSessionId, Int32 Generation)
    {

        /// <summary>
        /// Converts a tuple of a runner number(<see cref="int"/>), an ActiveSession identifier (<see cref="string"/>) 
        /// and a genration number (<see cref="int"/>) to an ExtRunnerKey value.
        /// </summary>
        /// <param name="Value">The tuple to be converted.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator  ExtRunnerKey((Int32 RunnerNumber, String ActiveSessionId, Int32 Generation) Value)
        {
            return new ExtRunnerKey(Value.RunnerNumber, Value.ActiveSessionId, Value.Generation);
        }

        /// <summary>
        /// Converts a tuple of an ActiveSession reference(<see cref="IActiveSession"/>) and a runner number (<see cref="int"/>) 
        /// to an ExtRunnerKey value.
        /// </summary>
        /// <param name="Value">The tuple to be converted.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator ExtRunnerKey((IActiveSession ActiveSession, Int32 RunnerNumber) Value)
        {
            return (Value.RunnerNumber, Value.ActiveSession.Id, Value.ActiveSession.Generation);
        }

        /// <summary>
        /// Make string representation of this instance.
        /// </summary>
        /// <returns>
        /// A string of form <format>"{<see cref="RunnerNumber"/>}-{<see cref="Generation"/>-<see cref="ActiveSessionId"/>}"</format>>
        /// </returns>
        public override String ToString()
        {
            return $"{RunnerNumber}-{Generation}-{HttpUtility.UrlEncode(ActiveSessionId)}";
        }

        /// <summary>
        /// Check if this instnce was issued for the ActiveSession passed as the parameter.
        /// </summary>
        /// <param name="Session">The ActiveSession to check affiliation with.</param>
        /// <returns>
        /// <see langword="true"/> if this instance belongs to an ActiveSession passed by <paramref name="Session"/> parameter,
        /// <see langword="false"/> otherwise
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Boolean IsForSession(IActiveSession Session)
        {
           if(Session == null) throw new ArgumentNullException(nameof(Session));
           return Session.Id==ActiveSessionId && Session.Generation==Generation;
        }

        /// <summary>
        /// A regular expression pattern used to parse the string representation of a value of this type.
        /// </summary>
        public const String RunnerKeyTemplate = @"^(\d+)-(\d+)-(.+)$";

        /// <summary>
        /// A static method that can be used for parsing a string representation of a value of this type.
        /// </summary>
        /// <param name="Source">The string to parse.</param>
        /// <param name="ExtRunnerKey">The parse result if the parsing operation was successful.</param>
        /// <returns>A value showing was the parsing operation successful.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Boolean TryParse(String Source, out ExtRunnerKey ExtRunnerKey)
        {
            Int32 num, gen;
            if(Source == null) throw new ArgumentNullException(nameof(Source));
            ExtRunnerKey=default;
            Match key_parts = Regex.Match(Source, RunnerKeyTemplate);
            if(key_parts.Success
                && key_parts.Groups.Count==4
                && Int32.TryParse(key_parts.Groups[1].Value, out num)
                && Int32.TryParse(key_parts.Groups[2].Value, out gen)) {
                    ExtRunnerKey=(num, HttpUtility.UrlDecode(key_parts.Groups[3].Value), gen);
                    return true;
            }
            else return false;
        }

        /// <summary>
        /// <inheritdoc cref="TryParse(string, out ExtRunnerKey)" path='/summary'/>
        /// </summary>
        /// <param name="Source">
        /// <inheritdoc cref="TryParse(string, out ExtRunnerKey)" path='/param[@name="Source"]'/>
        /// </param>
        /// <returns>
        /// <inheritdoc cref="TryParse(string, out ExtRunnerKey)" path='/param[@name="ExtRunnerKey"]'/>
        /// <br/>Otherwise a <see cref="FormatException"/> will be thrown.
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FormatException"></exception>
        public static ExtRunnerKey Parse(String Source)
        {
            ExtRunnerKey result;
            if(TryParse(Source, out result)) return result;
            else throw new FormatException("Bad RunnerKey format:"+Source);
        }

    }
}
