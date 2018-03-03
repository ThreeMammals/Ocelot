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
using Ocelot.DownstreamRouteFinder.Middleware;

namespace Ocelot.RequestId.Middleware
{
    public class ReRouteRequestIdMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public ReRouteRequestIdMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory, 
            IRequestScopedDataRepository requestScopedDataRepository)
        {
            _next = next;
            _requestScopedDataRepository = requestScopedDataRepository;
            _logger = loggerFactory.CreateLogger<ReRouteRequestIdMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            SetOcelotRequestId(context);
            await _next.Invoke(context);
        }

        private void SetOcelotRequestId(DownstreamContext context)
        {
            // if get request ID is set on upstream request then retrieve it
            var key = context.DownstreamReRoute.RequestIdKey ?? DefaultRequestIdKey.Value;
            
            StringValues upstreamRequestIds;
            if (context.HttpContext.Request.Headers.TryGetValue(key, out upstreamRequestIds))
            {
                //set the traceidentifier
                context.HttpContext.TraceIdentifier = upstreamRequestIds.First();

                //todo fix looking in both places
                //check if we have previous id in scoped repo
                var previousRequestId = _requestScopedDataRepository.Get<string>("RequestId");
                if (!previousRequestId.IsError && !string.IsNullOrEmpty(previousRequestId.Data))
                {
                    //we have a previous request id lets store it and update request id
                    _requestScopedDataRepository.Add<string>("PreviousRequestId", previousRequestId.Data);
                    _requestScopedDataRepository.Update<string>("RequestId", context.HttpContext.TraceIdentifier);
                }
                else
                {
                    //else just add request id
                    _requestScopedDataRepository.Add<string>("RequestId", context.HttpContext.TraceIdentifier);
                }
            }

            // set request ID on downstream request, if required
            var requestId = new RequestId(context.DownstreamReRoute.RequestIdKey, context.HttpContext.TraceIdentifier);

            if (ShouldAddRequestId(requestId, context.DownstreamRequest.Headers))
            {
                AddRequestIdHeader(requestId, context.DownstreamRequest);
            }
        }

        private bool ShouldAddRequestId(RequestId requestId, HttpRequestHeaders headers)
        {
            return !string.IsNullOrEmpty(requestId?.RequestIdKey)
                   && !string.IsNullOrEmpty(requestId.RequestIdValue)
                   && !RequestIdInHeaders(requestId, headers);
        }

        private bool RequestIdInHeaders(RequestId requestId, HttpRequestHeaders headers)
        {
            IEnumerable<string> value;
            return headers.TryGetValues(requestId.RequestIdKey, out value);
        }

        private void AddRequestIdHeader(RequestId requestId, HttpRequestMessage httpRequestMessage)
        {
            httpRequestMessage.Headers.Add(requestId.RequestIdKey, requestId.RequestIdValue);
        }
    }
}
