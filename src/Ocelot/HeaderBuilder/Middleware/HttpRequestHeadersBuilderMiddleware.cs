using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.DownstreamRouteFinder;
using Ocelot.Library.Middleware;
using Ocelot.Library.ScopedData;

namespace Ocelot.Library.HeaderBuilder.Middleware
{
    public class HttpRequestHeadersBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddHeadersToRequest _addHeadersToRequest;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;

        public HttpRequestHeadersBuilderMiddleware(RequestDelegate next, 
            IScopedRequestDataRepository scopedRequestDataRepository,
            IAddHeadersToRequest addHeadersToRequest) 
            : base(scopedRequestDataRepository)
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
            _scopedRequestDataRepository = scopedRequestDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _scopedRequestDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.Data.ReRoute.ClaimsToHeaders.Any())
            {
                _addHeadersToRequest.SetHeadersOnContext(downstreamRoute.Data.ReRoute.ClaimsToHeaders, context);
            }
            
            await _next.Invoke(context);
        }
    }
}
