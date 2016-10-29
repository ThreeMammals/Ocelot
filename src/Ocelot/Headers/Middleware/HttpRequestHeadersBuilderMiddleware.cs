using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.Headers.Middleware
{
    public class HttpRequestHeadersBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddHeadersToRequest _addHeadersToRequest;

        public HttpRequestHeadersBuilderMiddleware(RequestDelegate next, 
            IRequestScopedDataRepository requestScopedDataRepository,
            IAddHeadersToRequest addHeadersToRequest) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
        }

        public async Task Invoke(HttpContext context)
        {
            if (DownstreamRoute.ReRoute.ClaimsToHeaders.Any())
            {
                _addHeadersToRequest.SetHeadersOnContext(DownstreamRoute.ReRoute.ClaimsToHeaders, context);
            }
            
            await _next.Invoke(context);
        }
    }
}
