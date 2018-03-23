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

            context.DownstreamRequest.Scheme = context.DownstreamReRoute.DownstreamScheme;

            if (ServiceFabricRequest(context))
            {
                var pathAndQuery = CreateServiceFabricUri(context, dsPath);
                context.DownstreamRequest.AbsolutePath = pathAndQuery.path;
                context.DownstreamRequest.Query = pathAndQuery.query;
            }
            else
            {
                context.DownstreamRequest.AbsolutePath = dsPath.Data.Value;
            }

            _logger.LogDebug("downstream url is {context.DownstreamRequest}", context.DownstreamRequest);

            await _next.Invoke(context);
        }

        private (string path, string query) CreateServiceFabricUri(DownstreamContext context, Response<DownstreamPath> dsPath)
        {
            var query = context.DownstreamRequest.Query;           
            var serviceFabricPath = $"/{context.DownstreamReRoute.ServiceName + dsPath.Data.Value}";

            if (RequestForStatefullService(query))
            {
                return (serviceFabricPath, query);
            }

            var split = string.IsNullOrEmpty(query) ? "?" : "&";
            return (serviceFabricPath, $"{query}{split}cmd=instance");
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
