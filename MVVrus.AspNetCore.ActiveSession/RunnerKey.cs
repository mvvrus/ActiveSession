using System.Text.RegularExpressions;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The type to be used to pass a runner identifier into a code external to an ActiveSession library.
    /// </summary>
    /// <param name="RunnerNumber">A number assigned to the runner within the Active Session it belongs to.</param>
    /// <param name="Generation">Generation property value of the ActiveSession</param>
    /// <remarks>
    /// A purpose of this type is to avoid possible confusions if an Active session was changed between HTTP requests.
    ///<br/> The string represetation of an identifier (returned by its <see cref="ToString"/> method) has the form 
    /// <br/>"{<see cref="Generation"/>-{<see cref="RunnerNumber"/>"
    /// </remarks>
    public record struct RunnerKey(Int32 RunnerNumber, Int32 Generation)
    {

        /// <summary>
        /// Converts a tuple of two integers to a RunnerKey value.
        /// </summary>
        /// <param name="Value">The tuple to be converted.</param>
        public static implicit operator  RunnerKey((Int32 RunnerNumber, Int32 Generation) Value)
        {
            return new RunnerKey(Value.RunnerNumber, Value.Generation);
        }

        /// <summary>
        /// Make string representation of this instance.
        /// </summary>
        /// <returns>
        /// String "{<see cref="Generation"/>}-{<see cref="RunnerNumber"/>}" if the instance has a value
        /// or "&lt;Unknown RunnerId&gt;" if the instance is unassigned (has the default value).
        /// </returns>
        public override String ToString()
        {
            return $"{Generation}-{RunnerNumber}";
        }

        /// <summary>
        /// A regular expression pattern used to parse the string representation of a value of this type
        /// </summary>
        public const String RunnerKeyTemplate = @"^(\d+)-(\d+)$";

        /// <summary>
        /// A static method that can be used for parsing the string representation of a value of this type
        /// </summary>
        /// <param name="Source">The string to parse.</param>
        /// <param name="RunnerKey">Parse result if the parsing operation was successful.</param>
        /// <returns>A value showing was the parsing operation successful.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Boolean TryParse(String Source, out RunnerKey RunnerKey)
        {
            Int32 num, gen;
            if(Source == null) throw new ArgumentNullException(nameof(Source));
            RunnerKey=default;
            Match key_parts = Regex.Match(Source, RunnerKeyTemplate);
            if(key_parts.Success
                && key_parts.Groups.Count==3
                && Int32.TryParse(key_parts.Groups[1].Value, out gen)
                && Int32.TryParse(key_parts.Groups[2].Value, out num)) {
                RunnerKey=(num, gen);
                return true;
            }
            else return false;
        }

        /// <summary>
        /// <inheritdoc cref="TryParse(string, out RunnerKey)" path='/summary'/>
        /// </summary>
        /// <param name="Source">
        /// <inheritdoc cref="TryParse(string, out RunnerKey)" path='/param[@name="Source"]'/>
        /// </param>
        /// <returns>
        /// <inheritdoc cref="TryParse(string, out RunnerKey)" path='/param[@name="RunnerKey]'/>
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FormatException"></exception>
        public static RunnerKey Parse(String Source)
        {
            RunnerKey result;
            if(TryParse(Source, out result)) return result;
            else throw new FormatException("Bad RunnerKey format:"+Source);
        }

    }
}
