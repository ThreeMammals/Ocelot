using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.RequestId.Middleware
{
    public class RequestIdMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestIdMiddleware(RequestDelegate next, 
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            SetTraceIdentifier(context);

            await _next.Invoke(context);
        }

        private void SetTraceIdentifier(HttpContext context)
        {
            var key = DefaultRequestIdKey.Value;

            if (DownstreamRoute.ReRoute.RequestIdKey != null)
            {
                key = DownstreamRoute.ReRoute.RequestIdKey;
            }

            StringValues requestId;

            if (context.Request.Headers.TryGetValue(key, out requestId))
            {
                context.TraceIdentifier = requestId;
            }
        }
    }
}