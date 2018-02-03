using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IOcelotLogger _logger;
        private readonly DiagnosticSource _diagnostics;

        public HttpRequesterMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpRequester requester, 
            IRequestScopedDataRepository requestScopedDataRepository,
            DiagnosticSource diagnostic)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requester = requester;
            _diagnostics = diagnostic;
            _logger = loggerFactory.CreateLogger<HttpRequesterMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (_diagnostics.IsEnabled("Ocelot.Responder.Middleware.HttpRequesterMiddlewareStarting"))
            {
                _diagnostics.Write("Ocelot.Responder.Middleware.HttpRequesterMiddlewareStarting",
                    new
                    {
                        httpContext = context,
                        request = Request
                    });
            }

            var response = await _requester.GetResponse(Request);

            if (response.IsError)
            {
                _logger.LogDebug("IHttpRequester returned an error, setting pipeline error");

                SetPipelineError(response.Errors);
                return;
            }

            _logger.LogDebug("setting http response message");

            SetHttpResponseMessageThisRequest(response.Data);

            if (_diagnostics.IsEnabled("Ocelot.Responder.Middleware.HttpRequesterMiddlewareEnd"))
            {
                _diagnostics.Write("Ocelot.Responder.Middleware.HttpRequesterMiddlewareEnd",
                    new
                    {
                        httpContext = context,
                        request = Request
                    });
            }
        }
    }
}
