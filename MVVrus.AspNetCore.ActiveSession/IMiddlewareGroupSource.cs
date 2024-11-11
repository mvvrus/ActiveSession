namespace MVVrus.AspNetCore.ActiveSession
{
    internal /* (future) change to public when implementing middleware filter grouping*/ interface IMiddlewareGroupSource
    {
        Object? Token { get; }
        Type FilterType { get; }
        void GroupInto (IMiddlewareFilter MiddlewareFilter);
    }
}
