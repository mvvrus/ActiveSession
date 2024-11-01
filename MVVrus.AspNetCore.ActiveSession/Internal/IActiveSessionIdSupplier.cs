namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionIdSupplier
    {
        String GetBaseActiveSessionId(ISession Session); 
    }
}
