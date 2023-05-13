namespace MVVrus.AspNetCore.ActiveSession
{
    public struct KeyedActiveSessionRunner<TResult>
    {
        public IActiveSessionRunner<TResult> Runner;
        public Int32 Key;
    }
}
