namespace MVVrus.AspNetCore.ActiveSession
{
    internal /* TODO(future) change to public */ interface IMiddlewareGroupSource
    {
        Object? Token { get; }
        Type FilterType { get; }
        void GroupInto (IMiddlewareFilter MiddlewareFilter);
    }
}
