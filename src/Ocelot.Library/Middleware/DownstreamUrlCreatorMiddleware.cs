using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Responder;
using Ocelot.Library.Infrastructure.UrlTemplateReplacer;

namespace Ocelot.Library.Middleware
{
    public class DownstreamUrlCreatorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamUrlTemplateVariableReplacer _urlReplacer;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IHttpResponder _responder;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next, 
            IDownstreamUrlTemplateVariableReplacer urlReplacer,
            IScopedRequestDataRepository scopedRequestDataRepository, 
            IHttpResponder responder)
        {
            _next = next;
            _urlReplacer = urlReplacer;
            _scopedRequestDataRepository = scopedRequestDataRepository;
            _responder = responder;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _scopedRequestDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.IsError)
            {
                await _responder.CreateNotFoundResponse(context);
                return;
            }

            var downstreamUrl = _urlReplacer.ReplaceTemplateVariables(downstreamRoute.Data);

            _scopedRequestDataRepository.Add("DownstreamUrl", downstreamUrl);
                
            await _next.Invoke(context);
        }
    }
}