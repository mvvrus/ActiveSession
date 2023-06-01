namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Attribute used to select or deny use of constructors for type-based runner factories 
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, Inherited=false)]
    public class ActiveSessionConstructorAttribute: Attribute
    {
        /// <value>
        /// Value indicating inclusion/exclusion of the constructor
        /// </value>
        public Boolean Use;

        /// <summary>
        /// A constructor of the attribute
        /// </summary>
        /// <param name="Use">Value to be assigned to the Use field of the attribute</param>
        public ActiveSessionConstructorAttribute(Boolean Use=true)
        {
            this.Use = Use;
        }
    }
}
