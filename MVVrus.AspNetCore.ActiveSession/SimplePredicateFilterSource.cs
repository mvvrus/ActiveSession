internal class SimplePredicateFilterSource : IMiddlewareFilterSource
{
    Func<HttpContext, Boolean> _predicate;

    public SimplePredicateFilterSource(Func<HttpContext, Boolean> Predicate)
    {
        _predicate=Predicate;
    }

    public Boolean HasSuffix => false;
    public Boolean IsGroupable => false;
    public Boolean Group(IMiddlewareFilter GroupFilter) { throw new InvalidOperationException(); }

    internal Func<HttpContext, Boolean> Predicate { get => _predicate; }

    public IMiddlewareFilter Create(Int32 Order)
    {
        return new MiddlewareFilter(_predicate, Order);
    }

    public static implicit operator SimplePredicateFilterSource(Func<HttpContext, Boolean> Predicate)
    {
        return new SimplePredicateFilterSource(Predicate);
    }

    class MiddlewareFilter : IMiddlewareFilter
    {
        Func<HttpContext, Boolean> _filter;
        Int32 _order;

        public MiddlewareFilter(Func<HttpContext, Boolean> Filter, Int32 Order)
        {
            _filter=Filter;
            _order=Order;
        }
        public Int32 MinOrder => _order;

        public (Boolean WasMapped, String? SessionSuffix, Int32 Order) Apply(HttpContext Context)
        {
            return (_filter(Context), null, _order);
        }
    }

}
