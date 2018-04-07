using System;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Ocelot.Request.Middleware;

namespace Ocelot.RequestId.Middleware
{
    public class ReRouteRequestIdMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public ReRouteRequestIdMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory, 
            IRequestScopedDataRepository requestScopedDataRepository)
                : base(loggerFactory.CreateLogger<ReRouteRequestIdMiddleware>())
        {
            _next = next;
            _requestScopedDataRepository = requestScopedDataRepository;
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

            if (context.HttpContext.Request.Headers.TryGetValue(key, out var upstreamRequestIds))
            {
                context.HttpContext.TraceIdentifier = upstreamRequestIds.First();

                //check if we have previous id in scoped repo
                var previousRequestId = _requestScopedDataRepository.Get<string>("RequestId");
                if (!previousRequestId.IsError && !string.IsNullOrEmpty(previousRequestId.Data) && previousRequestId.Data != context.HttpContext.TraceIdentifier)
                {
                    //we have a previous request id lets store it and update request id
                    _requestScopedDataRepository.Add("PreviousRequestId", previousRequestId.Data);
                    _requestScopedDataRepository.Update("RequestId", context.HttpContext.TraceIdentifier);
                }
                else
                {
                    //else just add request id
                    _requestScopedDataRepository.Add("RequestId", context.HttpContext.TraceIdentifier);
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

        private void AddRequestIdHeader(RequestId requestId, DownstreamRequest httpRequestMessage)
        {
            httpRequestMessage.Headers.Add(requestId.RequestIdKey, requestId.RequestIdValue);
        }
    }
}
