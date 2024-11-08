internal class SimplePredicateFilterSource : IMiddlewareFilterSource
{
    Func<HttpContext, Boolean> _predicate;
    String _prettyName = "PredicateFilter";

    public SimplePredicateFilterSource(Func<HttpContext, Boolean> Predicate, String? PredicateName = null)
    {
        _predicate=Predicate;
        _prettyName=(PredicateName??_prettyName);
    }

    public Boolean HasSuffix => false;

    internal Func<HttpContext, Boolean> Predicate { get => _predicate; }

    public IMiddlewareFilter Create(Int32 Order)
    {
        return new MiddlewareFilter(_predicate, _prettyName, Order);
    }

    public String GetPrettyName() { return _prettyName; }

    public static implicit operator SimplePredicateFilterSource(Func<HttpContext, Boolean> Predicate)
    {
        return new SimplePredicateFilterSource(Predicate);
    }

    class MiddlewareFilter : IMiddlewareFilter
    {
        Func<HttpContext, Boolean> _filter;
        Int32 _order;
        String _prettyName;

        public MiddlewareFilter(Func<HttpContext, Boolean> Filter, String PrettyName, Int32 Order)
        {
            _filter=Filter;
            _order=Order;
            _prettyName=PrettyName;
        }
        public Int32 MinOrder => _order;

        public (Boolean WasMapped, String? SessionSuffix, Int32 Order) Apply(HttpContext Context)
        {
            return (_filter(Context), null, _order);
        }

        public String GetPrettyName() { return _prettyName; }

    }

}
