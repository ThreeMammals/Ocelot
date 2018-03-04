using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System;
using System.Linq;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Responses;
using Ocelot.Values;

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
            
            if (ServiceFabricRequest(context))
            {
                uriBuilder = CreateServiceFabricUri(context, dsPath);
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

        private UriBuilder CreateServiceFabricUri(DownstreamContext context, Response<DownstreamPath> dsPath)
        {
            var query = context.DownstreamRequest.RequestUri.Query;
            var scheme = context.DownstreamReRoute.DownstreamScheme;
            var host = context.DownstreamRequest.RequestUri.Host;
            var port = context.DownstreamRequest.RequestUri.Port;
            var serviceFabricPath = $"/{context.DownstreamReRoute.ServiceName + dsPath.Data.Value}";

            Uri uri;

            if (RequestForStatefullService(query))
            {
                uri = new Uri($"{scheme}://{host}:{port}{serviceFabricPath}{query}");
            }
            else
            {
                var split = string.IsNullOrEmpty(query) ? "?" : "&";
                uri = new Uri($"{scheme}://{host}:{port}{serviceFabricPath}{query}{split}cmd=instance");
            }

            return new UriBuilder(uri);
        }

        private static bool ServiceFabricRequest(DownstreamContext context)
        {
            return context.ServiceProviderConfiguration.Type == "ServiceFabric" && context.DownstreamReRoute.UseServiceDiscovery;
        }

        private static bool RequestForStatefullService(string query)
        {
            return query.Contains("PartitionKind") && query.Contains("PartitionKey");
        }
    }
}
