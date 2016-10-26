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
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next, 
            IDownstreamUrlPathPlaceholderReplacer urlReplacer,
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _urlReplacer = urlReplacer;
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _requestScopedDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            var downstreamUrl = _urlReplacer.Replace(downstreamRoute.Data.ReRoute.DownstreamTemplate, downstreamRoute.Data.TemplatePlaceholderNameAndValues);

            if (downstreamUrl.IsError)
            {
                SetPipelineError(downstreamUrl.Errors);
                return;
            }

            _requestScopedDataRepository.Add("DownstreamUrl", downstreamUrl.Data.Value);
                
            await _next.Invoke(context);
        }
    }
}