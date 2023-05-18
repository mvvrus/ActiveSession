namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionRunnerInfo
    {
        public ActiveSession Session;
        public IDisposable? Disposable;
        public Int32 Number;
        public Boolean UnregisterNumber;
        public ActiveSessionRunnerInfo(ActiveSession Session, IDisposable? Disposable, Int32 Number, Boolean UnregisterNumber)
        {
            this.Session = Session;
            this.Disposable = Disposable;
            this.Number = Number;
            this.UnregisterNumber = UnregisterNumber;
        }
    }
}
