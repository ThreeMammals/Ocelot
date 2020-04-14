namespace Ocelot.Requester.Middleware
{
    using Ocelot.Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;
    using System.Net;
    using System.Net.Http;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Threading.Tasks;
    using Ocelot.Responses;

    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;

        public HttpRequesterMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpRequester requester,
            IRequestScopedDataRepository repo)
                : base(loggerFactory.CreateLogger<HttpRequesterMiddleware>(), repo)
        {
            _next = next;
            _requester = requester;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var response = await _requester.GetResponse(DownstreamContext.Data, httpContext);

            CreateLogBasedOnResponse(response);

            if (response.IsError)
            {
                Logger.LogDebug("IHttpRequester returned an error, setting pipeline error");

                SetPipelineError(httpContext, response.Errors);
                return;
            }

            Logger.LogDebug("setting http response message");

            DownstreamContext.Data.DownstreamResponse = new DownstreamResponse(response.Data);

            await _next.Invoke(httpContext);
        }

        private void CreateLogBasedOnResponse(Response<HttpResponseMessage> response)
        {
            if (response.Data?.StatusCode <= HttpStatusCode.BadRequest)
            {
                Logger.LogInformation(
                    $"{(int)response.Data.StatusCode} ({response.Data.ReasonPhrase}) status code, request uri: {response.Data.RequestMessage?.RequestUri}");
            } 
            else if (response.Data?.StatusCode >= HttpStatusCode.BadRequest)
            {
                Logger.LogWarning(
                    $"{(int) response.Data.StatusCode} ({response.Data.ReasonPhrase}) status code, request uri: {response.Data.RequestMessage?.RequestUri}");
            }
        }
    }
}
