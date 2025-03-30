namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IStoreActiveSessionItem: IActiveSession, IActiveSessionServicesHelper, IDisposable
    {
        public IRunnerManager RunnerManager { get; }
        public IStoreGroupItem? BaseGroup { get; }
    }
}
