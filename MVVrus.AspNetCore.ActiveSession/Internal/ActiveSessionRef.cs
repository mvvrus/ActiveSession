namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionRef : SessionRef
    {
        //Provides an access to current ActiveSession object during resolving services from a DI container
        internal /*Just for tests*/ActiveSessionRef():base(null!) { } 

        public ActiveSessionRef(IServiceProvider RequestServices):base(RequestServices) { } 

        public void Initialize(Func<ILocalSession>? GetSession)
        {
            _getSessionFunc = GetSession;
        }

    }
}
