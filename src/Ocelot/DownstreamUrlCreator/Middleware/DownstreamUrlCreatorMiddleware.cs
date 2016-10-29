using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamUrlPathPlaceholderReplacer _urlReplacer;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next, 
            IDownstreamUrlPathPlaceholderReplacer urlReplacer,
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _urlReplacer = urlReplacer;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamUrl = _urlReplacer.Replace(DownstreamRoute.ReRoute.DownstreamTemplate, DownstreamRoute.TemplatePlaceholderNameAndValues);

            if (downstreamUrl.IsError)
            {
                SetPipelineError(downstreamUrl.Errors);
                return;
            }

            SetDownstreamUrlForThisRequest(downstreamUrl.Data.Value);

            await _next.Invoke(context);
        }
    }
}