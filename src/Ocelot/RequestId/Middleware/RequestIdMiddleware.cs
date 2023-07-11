using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Ocelot.Infrastructure.RequestData;

using Ocelot.Logging;

using Microsoft.AspNetCore.Http;

using Ocelot.Middleware;
using Ocelot.Request.Middleware;

namespace Ocelot.RequestId.Middleware
{
    public class RequestIdMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;
        public RequestIdMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository)
                : base(loggerFactory.CreateLogger<RequestIdMiddleware>())
        {
            _next = next;
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            SetOcelotRequestId(httpContext);
            await _next.Invoke(httpContext);
        }

        private void SetOcelotRequestId(HttpContext httpContext)
        {
            var downstreamRoute = httpContext.Items.DownstreamRoute();

            var key = downstreamRoute.RequestIdKey ?? DefaultRequestIdKey.Value;

            if (httpContext.Request.Headers.TryGetValue(key, out var upstreamRequestIds))
            {
                httpContext.TraceIdentifier = upstreamRequestIds.First();

                var previousRequestId = _requestScopedDataRepository.Get<string>("RequestId");
                if (!previousRequestId.IsError && !string.IsNullOrEmpty(previousRequestId.Data) && previousRequestId.Data != httpContext.TraceIdentifier)
                {
                    _requestScopedDataRepository.Add("PreviousRequestId", previousRequestId.Data);
                    _requestScopedDataRepository.Update("RequestId", httpContext.TraceIdentifier);
                }
                else
                {
                    _requestScopedDataRepository.Add("RequestId", httpContext.TraceIdentifier);
                }
            }

            var requestId = new RequestId(downstreamRoute.RequestIdKey, httpContext.TraceIdentifier);

            var downstreamRequest = httpContext.Items.DownstreamRequest();

            if (ShouldAddRequestId(requestId, downstreamRequest.Headers))
            {
                AddRequestIdHeader(requestId, downstreamRequest);
            }
        }

        private static bool ShouldAddRequestId(RequestId requestId, HttpRequestHeaders headers)
        {
            return !string.IsNullOrEmpty(requestId?.RequestIdKey)
                   && !string.IsNullOrEmpty(requestId.RequestIdValue)
                   && !RequestIdInHeaders(requestId, headers);
        }

        private static bool RequestIdInHeaders(RequestId requestId, HttpRequestHeaders headers)
        {
            return headers.TryGetValues(requestId.RequestIdKey, out var value);
        }

        private static void AddRequestIdHeader(RequestId requestId, DownstreamRequest httpRequestMessage)
        {
            httpRequestMessage.Headers.Add(requestId.RequestIdKey, requestId.RequestIdValue);
        }
    }
}
