using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Ocelot.Library.DownstreamRouteFinder;
using Ocelot.Library.RequestBuilder;

namespace Ocelot.Library.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Repository;

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

            if (downstreamRoute.Data.ReRoute.ConfigurationHeaderExtractorProperties.Any())
            {
                _addHeadersToRequest.SetHeadersOnContext(downstreamRoute.Data.ReRoute.ConfigurationHeaderExtractorProperties, context);
            }
            
            await _next.Invoke(context);
        }
    }
}
