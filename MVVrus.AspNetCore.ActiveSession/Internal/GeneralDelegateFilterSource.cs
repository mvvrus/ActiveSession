
namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class GeneralDelegateFilterSource : IMiddlewareFilterSource
    {
        Func<HttpContext, (Boolean, String?)> _filterDelegate;
        public Boolean HasSuffix => true;

        public GeneralDelegateFilterSource(Func<HttpContext, (Boolean, String?)> FilterDelegate)
        {
            _filterDelegate=FilterDelegate;
        }

        public IMiddlewareFilter Create(Int32 Order)
        {
            return new GeneralDelegateFilter(Order, _filterDelegate);
        }

        internal class GeneralDelegateFilter : IMiddlewareFilter
        {
            Int32 _order;
            Func<HttpContext, (Boolean, String?)> _filterDelegate;

            public GeneralDelegateFilter(Int32 Order, Func<HttpContext, (Boolean, String?)> FilterDelegate)
            {
                _order=Order;
                _filterDelegate=FilterDelegate;
            }

            public Int32 MinOrder => _order;

            public (Boolean WasMapped, String? SessionSuffix, Int32 Order) Apply(HttpContext Context)
            {
                (Boolean was_mapped, String? suffix) = _filterDelegate(Context);
                return (was_mapped, suffix, _order);
            }
        }
    }
}
