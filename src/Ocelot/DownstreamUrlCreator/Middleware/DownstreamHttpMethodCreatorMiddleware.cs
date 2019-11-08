using Ocelot.Logging;
using Ocelot.Middleware;
using System.Threading.Tasks;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public class DownstreamHttpMethodCreatorMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;

        public DownstreamHttpMethodCreatorMiddleware(OcelotRequestDelegate next, IOcelotLoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<DownstreamHttpMethodCreatorMiddleware>())
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
