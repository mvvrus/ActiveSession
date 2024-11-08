namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class PredicateWithSuffixFilterSource : IMiddlewareFilterSource
    {
        Func<HttpContext, Boolean> _predicate;
        String _suffix;
        String _prettyName = "PredicateFilter";

        public PredicateWithSuffixFilterSource(Func<HttpContext, Boolean> Predicate, String Suffix, String? PredicateName = null)
        {
            _predicate=Predicate;
            _suffix=Suffix;
            _prettyName=(PredicateName??_prettyName)+"->"+Suffix;
        }

        public Boolean HasSuffix => true;

        internal Func<HttpContext, Boolean> Predicate { get => _predicate; }
        internal String Suffix { get => _suffix; }

        public String GetPrettyName() { return _prettyName; }

        public IMiddlewareFilter Create(Int32 Order)
        {
            return new MiddlewareFilter(_predicate, _suffix, _prettyName, Order);
        }

        class MiddlewareFilter : IMiddlewareFilter
        {
            Func<HttpContext, Boolean> _filter;
            String _suffix;
            Int32 _order;
            String _prettyName;

            public MiddlewareFilter(Func<HttpContext, Boolean> Filter, String Suffix, String PrettyName, Int32 Order)
            {
                _filter=Filter;
                _suffix = Suffix;
                _order=Order;
                _prettyName=PrettyName;
            }
            public Int32 MinOrder => _order;

            public String GetPrettyName() { return _prettyName; }


            public (Boolean WasMapped, String? SessionSuffix, Int32 Order) Apply(HttpContext Context)
            {
                Boolean passed = _filter(Context);
                return (passed, passed ? _suffix : null, _order);
            }
        }

    }
}
