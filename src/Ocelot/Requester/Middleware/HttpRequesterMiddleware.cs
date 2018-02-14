using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Threading.Tasks;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IOcelotLogger _logger;

        public HttpRequesterMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpRequester requester, 
            IRequestScopedDataRepository requestScopedDataRepository
)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requester = requester;
            _logger = loggerFactory.CreateLogger<HttpRequesterMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        { 
            var response = await _requester.GetResponse(Request);

            if (response.IsError)
            {
                _logger.LogDebug("IHttpRequester returned an error, setting pipeline error");

                SetPipelineError(response.Errors);
                return;
            }

            _logger.LogDebug("setting http response message");

            SetHttpResponseMessageThisRequest(response.Data);
        }
    }
}
