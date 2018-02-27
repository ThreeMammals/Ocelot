using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System;
using Ocelot.DownstreamRouteFinder.Middleware;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IDownstreamPathPlaceholderReplacer _replacer;
        private readonly IOcelotLogger _logger;
        private readonly IUrlBuilder _urlBuilder;

        public DownstreamUrlCreatorMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamPathPlaceholderReplacer replacer,
            IUrlBuilder urlBuilder)
        {
            _next = next;
            _replacer = replacer;
            _urlBuilder = urlBuilder;
            _logger = loggerFactory.CreateLogger<DownstreamUrlCreatorMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            var dsPath = _replacer
                .Replace(context.DownstreamReRoute.DownstreamPathTemplate, context.TemplatePlaceholderNameAndValues);

            if (dsPath.IsError)
            {
                _logger.LogDebug("IDownstreamPathPlaceholderReplacer returned an error, setting pipeline error");

                SetPipelineError(context, dsPath.Errors);
                return;
            }

            var uriBuilder = new UriBuilder(context.DownstreamRequest.RequestUri)
            {
                Path = dsPath.Data.Value,
                Scheme = context.DownstreamReRoute.DownstreamScheme
            };

            context.DownstreamRequest.RequestUri = uriBuilder.Uri;

            _logger.LogDebug("downstream url is {downstreamUrl.Data.Value}", context.DownstreamRequest.RequestUri);

            await _next.Invoke(context);
        }
    }
}
