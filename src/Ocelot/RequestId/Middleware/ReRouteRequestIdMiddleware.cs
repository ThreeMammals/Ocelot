using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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
            var key = context.DownstreamReRoute.RequestIdKey ?? DefaultRequestIdKey.Value;

            if (context.HttpContext.Request.Headers.TryGetValue(key, out var upstreamRequestIds))
            {
                context.HttpContext.TraceIdentifier = upstreamRequestIds.First();

                var previousRequestId = _requestScopedDataRepository.Get<string>("RequestId");
                if (!previousRequestId.IsError && !string.IsNullOrEmpty(previousRequestId.Data) && previousRequestId.Data != context.HttpContext.TraceIdentifier)
                {
                    _requestScopedDataRepository.Add("PreviousRequestId", previousRequestId.Data);
                    _requestScopedDataRepository.Update("RequestId", context.HttpContext.TraceIdentifier);
                }
                else
                {
                    _requestScopedDataRepository.Add("RequestId", context.HttpContext.TraceIdentifier);
                }
            }

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
