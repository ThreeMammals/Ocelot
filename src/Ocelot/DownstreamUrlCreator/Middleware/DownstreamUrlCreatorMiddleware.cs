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

        public DownstreamUrlCreatorMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamPathPlaceholderReplacer replacer)
                :base(loggerFactory.CreateLogger<DownstreamUrlCreatorMiddleware>())
        {
            _next = next;
            _replacer = replacer;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var dsPath = _replacer
                .Replace(context.DownstreamReRoute.DownstreamPathTemplate, context.TemplatePlaceholderNameAndValues);

            if (dsPath.IsError)
            {
                Logger.LogDebug("IDownstreamPathPlaceholderReplacer returned an error, setting pipeline error");

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

            Logger.LogDebug($"Downstream url is {context.DownstreamRequest}");

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
            return context.Configuration.ServiceProviderConfiguration.Type == "ServiceFabric" && context.DownstreamReRoute.UseServiceDiscovery;
        }

        private static bool RequestForStatefullService(string query)
        {
            return query.Contains("PartitionKind") && query.Contains("PartitionKey");
        }
    }
}
