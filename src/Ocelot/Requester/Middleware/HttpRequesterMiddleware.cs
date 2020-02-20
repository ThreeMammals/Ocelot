using System.Net;
using System.Net.Http;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Threading.Tasks;
using Ocelot.Responses;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IHttpRequester _requester;

        public HttpRequesterMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpRequester requester)
                : base(loggerFactory.CreateLogger<HttpRequesterMiddleware>())
        {
            _next = next;
            _requester = requester;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var response = await _requester.GetResponse(context);

            CreateLogBasedOnResponse(response);

            if (response.IsError)
            {
                Logger.LogDebug("IHttpRequester returned an error, setting pipeline error");

                SetPipelineError(context, response.Errors);
                return;
            }

            Logger.LogDebug("setting http response message");

            context.DownstreamResponse = new DownstreamResponse(response.Data);

            await _next.Invoke(context);
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
