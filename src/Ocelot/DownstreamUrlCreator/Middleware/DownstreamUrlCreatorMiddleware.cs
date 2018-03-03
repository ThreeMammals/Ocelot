using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System;
using System.Linq;
using Ocelot.DownstreamRouteFinder.Middleware;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IDownstreamPathPlaceholderReplacer _replacer;
        private readonly IOcelotLogger _logger;

        public DownstreamUrlCreatorMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamPathPlaceholderReplacer replacer)
        {
            _next = next;
            _replacer = replacer;
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

            UriBuilder uriBuilder;
            
            //todo - feel this is a bit crap the way we build the url dont see why we need this builder thing..maybe i blew my own brains out 
            // when i originally wrote it..
            if (context.ServiceProviderConfiguration.Type == "ServiceFabric" && context.DownstreamReRoute.UseServiceDiscovery)
            {
                _logger.LogInformation("DownstreamUrlCreatorMiddleware - going to try set service fabric path");

                var scheme = context.DownstreamReRoute.DownstreamScheme;
                var host = context.DownstreamRequest.RequestUri.Host;
                var port = context.DownstreamRequest.RequestUri.Port;
                var serviceFabricPath = $"/{context.DownstreamReRoute.ServiceName + dsPath.Data.Value}";

                _logger.LogInformation("DownstreamUrlCreatorMiddleware - service fabric path is {proxyUrl}", serviceFabricPath);

                var uri = new Uri($"{scheme}://{host}:{port}{serviceFabricPath}?cmd=instance");
                uriBuilder = new UriBuilder(uri);
            }
            else
            {
                uriBuilder = new UriBuilder(context.DownstreamRequest.RequestUri)
                {
                    Path = dsPath.Data.Value,
                    Scheme = context.DownstreamReRoute.DownstreamScheme
                };
            }

            context.DownstreamRequest.RequestUri = uriBuilder.Uri;

            _logger.LogDebug("downstream url is {downstreamUrl.Data.Value}", context.DownstreamRequest.RequestUri);

            await _next.Invoke(context);
        }
    }
}
