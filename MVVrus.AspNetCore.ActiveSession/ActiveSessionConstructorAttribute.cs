namespace MVVrus.AspNetCore.ActiveSession
{
    [AttributeUsage(AttributeTargets.Constructor, Inherited=false)]
    public class ActiveSessionConstructorAttribute: Attribute
    {
        public Boolean Use;
        public ActiveSessionConstructorAttribute(Boolean Use=true)
        {
            this.Use = Use;
        }
    }
}
