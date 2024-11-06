internal class PredicateWithSuffixFilterSource : IMiddlewareFilterSource
{
    Func<HttpContext, Boolean> _predicate;
    String _suffix;

    public PredicateWithSuffixFilterSource(Func<HttpContext, Boolean> Predicate, String Suffix)
    {
        _predicate=Predicate;
        _suffix=Suffix;
    }

    public Boolean HasSuffix => true;
    public Boolean IsGroupable => false;
    public Boolean Group(IMiddlewareFilter GroupFilter) { throw new InvalidOperationException(); }

    internal Func<HttpContext, Boolean> Predicate { get => _predicate; }
    internal String Suffix { get => _suffix; }

    public IMiddlewareFilter Create(Int32 Order)
    {
        return new MiddlewareFilter(_predicate, _suffix, Order);
    }

    class MiddlewareFilter : IMiddlewareFilter
    {
        Func<HttpContext, Boolean> _filter;
        String _suffix;
        Int32 _order;

        public MiddlewareFilter(Func<HttpContext, Boolean> Filter, String Suffix, Int32 Order)
        {
            _filter=Filter;
            _suffix = Suffix;
            _order=Order;
        }
        public Int32 MinOrder => _order;

        public (Boolean WasMapped, String? SessionSuffix, Int32 Order) Apply(HttpContext Context)
        {
            Boolean passed = _filter(Context);
            return (passed, passed?_suffix:null, _order);
        }
    }

}
