namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class RunnerInfo
    {
        public IRunner Runner { get; init; }
        public Type ResultType { get; init; }
        public String? RemoteHost { get; init; } //Currently not used and always null.
        public Int32 Number { get; init; }

        public RunnerInfo(IRunner Runner, Type ResultType, Int32 Number)
        {
            this.Runner=Runner;
            this.ResultType=ResultType;
            this.Number=Number;
        }
    }
}
