namespace Ocelot.DownstreamUrlCreator.Middleware
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Ocelot.Configuration;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Request.Middleware;
    using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Responses;
    using Ocelot.Values;
    using System;
    using System.Threading.Tasks;

    public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamPathPlaceholderReplacer _replacer;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamPathPlaceholderReplacer replacer
            )
                : base(loggerFactory.CreateLogger<DownstreamUrlCreatorMiddleware>())
        {
            _next = next;
            _replacer = replacer;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            var downstreamReRoute = Get(httpContext, downstreamContext);

            var response = _replacer
                .Replace(downstreamReRoute.DownstreamPathTemplate.Value, downstreamContext.TemplatePlaceholderNameAndValues);

            if (response.IsError)
            {
                Logger.LogDebug("IDownstreamPathPlaceholderReplacer returned an error, setting pipeline error");

                SetPipelineError(downstreamContext, response.Errors);
                return;
            }

            if (!string.IsNullOrEmpty(downstreamReRoute.DownstreamScheme))
            {
                downstreamContext.DownstreamRequest.Scheme = downstreamReRoute.DownstreamScheme;
            }

            if (ServiceFabricRequest(downstreamContext.Configuration, downstreamReRoute))
            {
                var pathAndQuery = CreateServiceFabricUri(downstreamContext.DownstreamRequest, downstreamReRoute, downstreamContext.TemplatePlaceholderNameAndValues, response);
                downstreamContext.DownstreamRequest.AbsolutePath = pathAndQuery.path;
                downstreamContext.DownstreamRequest.Query = pathAndQuery.query;
            }
            else
            {
                var dsPath = response.Data;

                if (ContainsQueryString(dsPath))
                {
                    downstreamContext.DownstreamRequest.AbsolutePath = GetPath(dsPath);

                    if (string.IsNullOrEmpty(downstreamContext.DownstreamRequest.Query))
                    {
                        downstreamContext.DownstreamRequest.Query = GetQueryString(dsPath);
                    }
                    else
                    {
                        downstreamContext.DownstreamRequest.Query += GetQueryString(dsPath).Replace('?', '&');
                    }
                }
                else
                {
                    RemoveQueryStringParametersThatHaveBeenUsedInTemplate(downstreamContext.DownstreamRequest, downstreamContext.TemplatePlaceholderNameAndValues);

                    downstreamContext.DownstreamRequest.AbsolutePath = dsPath.Value;
                }
            }

            Logger.LogDebug($"Downstream url is {downstreamContext.DownstreamRequest}");

            await _next.Invoke(httpContext);
        }

        private static void RemoveQueryStringParametersThatHaveBeenUsedInTemplate(DownstreamRequest downstreamRequest, List<PlaceholderNameAndValue> templatePlaceholderNameAndValues)
        {
            foreach (var nAndV in templatePlaceholderNameAndValues)
            {
                var name = nAndV.Name.Replace("{", "").Replace("}", "");

                if (downstreamRequest.Query.Contains(name) &&
                    downstreamRequest.Query.Contains(nAndV.Value))
                {
                    var questionMarkOrAmpersand = downstreamRequest.Query.IndexOf(name, StringComparison.Ordinal);
                    downstreamRequest.Query = downstreamRequest.Query.Remove(questionMarkOrAmpersand - 1, 1);

                    var rgx = new Regex($@"\b{name}={nAndV.Value}\b");
                    downstreamRequest.Query = rgx.Replace(downstreamRequest.Query, "");

                    if (!string.IsNullOrEmpty(downstreamRequest.Query))
                    {
                        downstreamRequest.Query = '?' + downstreamRequest.Query.Substring(1);
                    }
                }
            }
        }

        private string GetPath(DownstreamPath dsPath)
        {
            return dsPath.Value.Substring(0, dsPath.Value.IndexOf("?", StringComparison.Ordinal));
        }

        private string GetQueryString(DownstreamPath dsPath)
        {
            return dsPath.Value.Substring(dsPath.Value.IndexOf("?", StringComparison.Ordinal));
        }

        private bool ContainsQueryString(DownstreamPath dsPath)
        {
            return dsPath.Value.Contains("?");
        }

        private (string path, string query) CreateServiceFabricUri(DownstreamRequest downstreamRequest, DownstreamReRoute downstreamReRoute, List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, Response<DownstreamPath> dsPath)
        {
            var query = downstreamRequest.Query;
            var serviceName = _replacer.Replace(downstreamReRoute.ServiceName, templatePlaceholderNameAndValues);
            var pathTemplate = $"/{serviceName.Data.Value}{dsPath.Data.Value}";
            return (pathTemplate, query);
        }

        private static bool ServiceFabricRequest(IInternalConfiguration config, DownstreamReRoute downstreamReRoute)
        {
            return config.ServiceProviderConfiguration.Type?.ToLower() == "servicefabric" && downstreamReRoute.UseServiceDiscovery;
        }
    }
}
