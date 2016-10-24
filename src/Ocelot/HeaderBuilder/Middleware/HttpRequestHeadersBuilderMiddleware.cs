using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.HeaderBuilder.Middleware
{
    public class HttpRequestHeadersBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddHeadersToRequest _addHeadersToRequest;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public HttpRequestHeadersBuilderMiddleware(RequestDelegate next, 
            IRequestScopedDataRepository requestScopedDataRepository,
            IAddHeadersToRequest addHeadersToRequest) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _requestScopedDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.Data.ReRoute.ClaimsToHeaders.Any())
            {
                _addHeadersToRequest.SetHeadersOnContext(downstreamRoute.Data.ReRoute.ClaimsToHeaders, context);
            }
            
            await _next.Invoke(context);
        }
    }
}
