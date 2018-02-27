using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Threading.Tasks;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IOcelotLogger _logger;

        public HttpRequesterMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpRequester requester)
        {
            _next = next;
            _requester = requester;
            _logger = loggerFactory.CreateLogger<HttpRequesterMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            var response = await _requester.GetResponse(context);

            if (response.IsError)
            {
                _logger.LogDebug("IHttpRequester returned an error, setting pipeline error");

                SetPipelineError(context, response.Errors);
                return;
            }

            _logger.LogDebug("setting http response message");

            context.DownstreamResponse = response.Data;
        }
    }
}
