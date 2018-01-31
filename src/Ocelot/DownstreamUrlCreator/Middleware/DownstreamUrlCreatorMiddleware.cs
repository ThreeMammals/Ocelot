using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamPathPlaceholderReplacer _replacer;
        private readonly IOcelotLogger _logger;
        private readonly IUrlBuilder _urlBuilder;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamPathPlaceholderReplacer replacer,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IUrlBuilder urlBuilder)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _replacer = replacer;
            _urlBuilder = urlBuilder;
            _logger = loggerFactory.CreateLogger<DownstreamUrlCreatorMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var dsPath = _replacer
                .Replace(DownstreamRoute.ReRoute.DownstreamPathTemplate, DownstreamRoute.TemplatePlaceholderNameAndValues);

            if (dsPath.IsError)
            {
                _logger.LogDebug("IDownstreamPathPlaceholderReplacer returned an error, setting pipeline error");

                SetPipelineError(dsPath.Errors);
                return;
            }

            var uriBuilder = new UriBuilder(DownstreamRequest.RequestUri)
            {
                Path = dsPath.Data.Value,
                Scheme = DownstreamRoute.ReRoute.DownstreamScheme
            };

            DownstreamRequest.RequestUri = uriBuilder.Uri;

            _logger.LogDebug("downstream url is {downstreamUrl.Data.Value}", DownstreamRequest.RequestUri);

            await _next.Invoke(context);
        }
    }
}