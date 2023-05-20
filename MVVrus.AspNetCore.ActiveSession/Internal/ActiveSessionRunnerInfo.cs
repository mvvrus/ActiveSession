namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionRunnerInfo
    {
        public ActiveSession RunnerSession;
        public IDisposable? Disposable;
        public Int32 Number;
        public Boolean UnregisterNumber;
        public ActiveSessionRunnerInfo(ActiveSession RunnerSession, IDisposable? Disposable, Int32 Number, Boolean UnregisterNumber)
        {
            this.RunnerSession = RunnerSession;
            this.Disposable = Disposable;
            this.Number = Number;
            this.UnregisterNumber = UnregisterNumber;
        }
    }
}
