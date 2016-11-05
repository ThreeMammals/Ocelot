using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamUrlPathPlaceholderReplacer _urlReplacer;
        private readonly IOcelotLogger _logger;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamUrlPathPlaceholderReplacer urlReplacer,
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _urlReplacer = urlReplacer;
            _logger = loggerFactory.CreateLogger<DownstreamUrlCreatorMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling downstream url creator middleware");

            var downstreamUrl = _urlReplacer.Replace(DownstreamRoute.ReRoute.DownstreamTemplate, DownstreamRoute.TemplatePlaceholderNameAndValues);

            if (downstreamUrl.IsError)
            {
                _logger.LogDebug("IDownstreamUrlPathPlaceholderReplacer returned an error, setting pipeline error");

                SetPipelineError(downstreamUrl.Errors);
                return;
            }

            _logger.LogDebug("downstream url is {downstreamUrl.Data.Value}", downstreamUrl.Data.Value);

            SetDownstreamUrlForThisRequest(downstreamUrl.Data.Value);

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}