using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace Ocelot.RequestId.Middleware
{
    public class RequestIdMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public RequestIdMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<RequestIdMiddleware>();
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {         
            _logger.TraceMiddlewareEntry();

            SetOcelotRequestId(context);

            _logger.TraceInvokeNext();
                await _next.Invoke(context);
            _logger.TraceInvokeNextCompleted();
            _logger.TraceMiddlewareCompleted();
        }

        private void SetOcelotRequestId(HttpContext context)
        {
            var key = DefaultRequestIdKey.Value;

            if (DownstreamRoute.ReRoute.RequestIdKey != null)
            {
                key = DownstreamRoute.ReRoute.RequestIdKey;
            }
            
            StringValues requestIds;

            if (context.Request.Headers.TryGetValue(key, out requestIds))
            {
                var requestId = requestIds.First();
                var downstreamRequestHeaders = DownstreamRequest.Headers;

                if (!string.IsNullOrEmpty(requestId) && 
                    !HeaderExists(key, downstreamRequestHeaders))
                {
                    downstreamRequestHeaders.Add(key, requestId);
                }

                context.TraceIdentifier = requestId;
            }
        }

        private bool HeaderExists(string headerKey, HttpRequestHeaders headers)
        {
            IEnumerable<string> value;
            return headers.TryGetValues(headerKey, out value);
        }
    }
}