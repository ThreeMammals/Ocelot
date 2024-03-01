using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;

        public HttpRequesterMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpRequester requester)
                : base(loggerFactory.CreateLogger<HttpRequesterMiddleware>())
        {
            _next = next;
            _requester = requester;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var response = await _requester.GetResponse(httpContext);
            CreateLogBasedOnResponse(response);

            if (response.IsError)
            {
                Logger.LogDebug("IHttpRequester returned an error, setting pipeline error");

                httpContext.Items.UpsertErrors(response.Errors);
                return;
            }

            Logger.LogDebug("setting http response message");
            httpContext.Items.UpsertDownstreamResponse(new DownstreamResponse(response.Data));
            await _next.Invoke(httpContext);
        }

        private void CreateLogBasedOnResponse(Response<HttpResponseMessage> response)
        {
            var status = response.Data?.StatusCode ?? HttpStatusCode.Processing;
            var reason = response.Data?.ReasonPhrase ?? "unknown";
            var uri = response.Data?.RequestMessage?.RequestUri?.ToString() ?? string.Empty;

            string message() => $"{(int)status} ({reason}) status code of request URI: {uri}.";

            if (status < HttpStatusCode.BadRequest)
            {
                Logger.LogInformation(message);
            }
            else
            {
                Logger.LogWarning(message);
            }
        }
    }
}
