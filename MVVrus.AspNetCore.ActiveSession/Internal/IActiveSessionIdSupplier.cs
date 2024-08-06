namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionIdSupplier
    {
        String GetActiveSessionId(ISession Session); 
    }
}
