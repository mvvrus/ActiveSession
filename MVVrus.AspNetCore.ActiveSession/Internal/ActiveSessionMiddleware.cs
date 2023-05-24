using Microsoft.AspNetCore.Http.Features;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionMiddleware
    {
        readonly RequestDelegate _next;
        readonly IActiveSessionStore _store;
        readonly ILogger _logger;

        public ActiveSessionMiddleware(RequestDelegate Next, IActiveSessionStore Store, ILoggerFactory LoggerFactory)
        {
            _next=Next??throw new ArgumentNullException(nameof(Next));
            _store=Store??throw new ArgumentNullException(nameof(Store));
            if (LoggerFactory is null) throw new ArgumentNullException(nameof(LoggerFactory));
            _logger = LoggerFactory.CreateLogger(LOGGING_CATEGORY_NAME); 
        }

        public async Task Invoke(HttpContext context)
        {
            //TODO Check that Features.Get<ISessionFeature>() does return non-null?
            IActiveSessionFeature feature = new ActiveSessionFeature(_store, context.Features.Get<ISessionFeature>()?.Session);
            //TODO Implement logging
            context.Features.Set(feature);
            try
            {
                await _next(context);
                await feature.CommitAsync();
            }
            finally
            {
                context.Features.Set<IActiveSessionFeature>(null);
                feature.Clear();
            }
        }
    }
}
