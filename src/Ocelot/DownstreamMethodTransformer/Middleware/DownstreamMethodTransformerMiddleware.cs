using Ocelot.Logging;
using Ocelot.Middleware;
using System.Threading.Tasks;

namespace Ocelot.DownstreamMethodTransformer.Middleware
{
    public class DownstreamMethodTransformerMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;

        public DownstreamMethodTransformerMiddleware(OcelotRequestDelegate next, IOcelotLoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<DownstreamMethodTransformerMiddleware>())
        {
            _next = next;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (context.DownstreamReRoute.DownstreamHttpMethod != null)
            {
                context.DownstreamRequest.Method = context.DownstreamReRoute.DownstreamHttpMethod;
            }

            await _next.Invoke(context);
        }
    }
}
