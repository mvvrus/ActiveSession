using System.Net;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The type to be used to pass a runner identifier into a code external to an ActiveSession library.
    /// </summary>
    /// <param name="RunnerNumber">A number assigned to the runner within the Active Session it belongs to.</param>
    /// <param name="ActiveSessionId">TODO</param>
    /// <param name="Generation">Generation property value of the ActiveSession</param>
    /// <remarks>
    /// A purpose of this type is to avoid possible confusions if an Active session was changed between HTTP requests.
    ///<br/> The string represetation of an identifier (returned by its <see cref="ToString"/> method) has the form 
    /// <br/>"{<see cref="Generation"/>-{<see cref="RunnerNumber"/>"
    /// </remarks>
    public record struct ExtRunnerKey(Int32 RunnerNumber, String ActiveSessionId, Int32 Generation)
    {

        /// <summary>
        /// Converts a tuple of runner number(<see cref="int"/>), ActiveSession identifier (<see cref="string"/>) and genration number (<see cref="int"/>) to an ExtRunnerKey value.
        /// </summary>
        /// <param name="Value">The tuple to be converted.</param>
        public static implicit operator  ExtRunnerKey((Int32 RunnerNumber, String ActiveSessionId, Int32 Generation) Value)
        {
            return new ExtRunnerKey(Value.RunnerNumber, Value.ActiveSessionId, Value.Generation);
        }

        /// <summary>
        /// Converts a tuple of ActiveSession reference(IActiveSession) and runner number (int) to an ExtRunnerKey value.
        /// </summary>
        /// <param name="Value">The tuple to be converted.</param>
        public static implicit operator ExtRunnerKey((IActiveSession ActiveSession, Int32 RunnerNumber) Value)
        {
            return (Value.RunnerNumber, Value.ActiveSession.Id, Value.ActiveSession.Generation);
        }

        /// <summary>
        /// Make string representation of this instance.
        /// </summary>
        /// <returns>
        /// String "{<see cref="Generation"/>}-{<see cref="RunnerNumber"/>-<see cref="ActiveSessionId"/>}" if the instance has a value
        /// </returns>
        public override String ToString()
        {
            return $"{RunnerNumber}-{Generation}-{HttpUtility.UrlEncode(ActiveSessionId)}";
        }

        /// <summary>
        /// A regular expression pattern used to parse the string representation of a value of this type
        /// </summary>
        public const String RunnerKeyTemplate = @"^(\d+)-(\d+)-(.+)$";

        /// <summary>
        /// A static method that can be used for parsing the string representation of a value of this type
        /// </summary>
        /// <param name="Source">The string to parse.</param>
        /// <param name="ExtRunnerKey">Parse result if the parsing operation was successful.</param>
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
        /// <inheritdoc cref="TryParse(string, out ExtRunnerKey)" path='/param[@name="RunnerKey]'/>
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
