using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Responder;
using Ocelot.Library.Infrastructure.Services;
using Ocelot.Library.Infrastructure.UrlTemplateReplacer;

namespace Ocelot.Library.Middleware
{
    public class DownstreamUrlCreatorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamUrlTemplateVariableReplacer _urlReplacer;
        private readonly IRequestDataService _requestDataService;
        private readonly IHttpResponder _responder;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next, 
            IDownstreamUrlTemplateVariableReplacer urlReplacer,
            IRequestDataService requestDataService, 
            IHttpResponder responder)
        {
            _next = next;
            _urlReplacer = urlReplacer;
            _requestDataService = requestDataService;
            _responder = responder;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _requestDataService.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.IsError)
            {
                await _responder.CreateNotFoundResponse(context);
                return;
            }

            var downstreamUrl = _urlReplacer.ReplaceTemplateVariables(downstreamRoute.Data);

            _requestDataService.Add("DownstreamUrl", downstreamUrl);
                
            await _next.Invoke(context);
        }
    }
}