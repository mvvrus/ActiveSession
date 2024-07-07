namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal record RunnerInfo
    {
        public readonly IRunner Runner; 
        public readonly Type ResultType;
        public readonly String? RemoteHost;  //Currently not used and always null.
        public readonly Int32 Number;
        public TaskCompletionSource? TrackCleanup;

        public RunnerInfo(IRunner Runner, Type ResultType, Int32 Number)
        {
            this.Runner=Runner;
            this.ResultType=ResultType;
            this.Number=Number;
        }
    }
}
