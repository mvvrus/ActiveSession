namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionFeatureControl
    {
        public void Clear();
        public void SetSession(ISession? Session, String? TraceIdentifier=null);

    }
}
