namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    //(future) One day I will invert dependency of ActiveSessionStore on ActiveSession using this class
    internal interface IStoreActiveSessionItem: IActiveSession, ISessionServicesHelper, IDisposable
    {
        public IRunnerManager RunnerManager { get; }
        public IStoreGroupItem? BaseGroup { get; }
    }
}
