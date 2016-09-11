using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ocelot.Library.Infrastructure.Configuration;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Requester;
using Ocelot.Library.Infrastructure.Responder;
using Ocelot.Library.Infrastructure.UrlTemplateReplacer;

namespace Ocelot.Library.Middleware
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamUrlTemplateVariableReplacer _urlReplacer;
        private readonly IOptions<Configuration> _configuration;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IHttpRequester _requester;
        private readonly IHttpResponder _responder;

        public ProxyMiddleware(RequestDelegate next, 
            IDownstreamUrlTemplateVariableReplacer urlReplacer, 
            IOptions<Configuration> configuration, 
            IDownstreamRouteFinder downstreamRouteFinder, 
            IHttpRequester requester, 
            IHttpResponder responder)
        {
            _next = next;
            _urlReplacer = urlReplacer;
            _configuration = configuration;
            _downstreamRouteFinder = downstreamRouteFinder;
            _requester = requester;
            _responder = responder;
        }

        public async Task Invoke(HttpContext context)
        {   
            var upstreamUrlPath = context.Request.Path.ToString();

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath);

            if (downstreamRoute.IsError)
            {
                await _responder.CreateNotFoundResponse(context);
                return;
            }

            var downstreamUrl = _urlReplacer.ReplaceTemplateVariables(downstreamRoute.Data);

            var response = await _requester.GetResponse(context.Request.Method, downstreamUrl);

            context = await _responder.CreateSuccessResponse(context, response);

            await _next.Invoke(context);
        }
    }
}