namespace MVVrus.AspNetCore.ActiveSession
{
    public record struct KeyedActiveSessionRunner<TResult>
    (
        IActiveSessionRunner<TResult> Runner,
        Int32 Key
    );
}
