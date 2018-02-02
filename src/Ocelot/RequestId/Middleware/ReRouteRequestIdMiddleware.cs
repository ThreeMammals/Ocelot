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
    public class ReRouteRequestIdMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public ReRouteRequestIdMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ReRouteRequestIdMiddleware>();
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            SetOcelotRequestId(context);
            await _next.Invoke(context);
        }

        private void SetOcelotRequestId(HttpContext context)
        {
            // if get request ID is set on upstream request then retrieve it
            var key = DownstreamRoute.ReRoute.RequestIdKey ?? DefaultRequestIdKey.Value;
            
            StringValues upstreamRequestIds;
            if (context.Request.Headers.TryGetValue(key, out upstreamRequestIds))
            {
                //set the traceidentifier
                context.TraceIdentifier = upstreamRequestIds.First();

                //check if we have previous id
                var previousRequestId = _requestScopedDataRepository.Get<string>("RequestId");
                if(!previousRequestId.IsError && !string.IsNullOrEmpty(previousRequestId.Data))
                {
                    //we have a previous request id lets store it and update request id
                    _requestScopedDataRepository.Add<string>("PreviousRequestId", previousRequestId.Data);
                    _requestScopedDataRepository.Update<string>("RequestId", context.TraceIdentifier);
                }
                else
                {
                    //else just add request id
                    _requestScopedDataRepository.Add<string>("RequestId", context.TraceIdentifier);
                }
            }

            // set request ID on downstream request, if required
            var requestId = new RequestId(DownstreamRoute?.ReRoute?.RequestIdKey, context.TraceIdentifier);

            if (ShouldAddRequestId(requestId, DownstreamRequest.Headers))
            {
                AddRequestIdHeader(requestId, DownstreamRequest);
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