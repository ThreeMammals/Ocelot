namespace Ocelot.DownstreamRouteFinder.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.Infrastructure.Extensions;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Middleware.Multiplexer;
    using System.Linq;
    using System.Threading.Tasks;
    using Configuration;
    using Configuration.Repository;
    using Infrastructure.RequestData;

    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteProviderFactory _factory;
        private readonly IMultiplexer _multiplexer;

        public DownstreamRouteFinderMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamRouteProviderFactory downstreamRouteFinder,
            IMultiplexer multiplexer,
            IRequestScopedDataRepository repo
            )
                : base(loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>(), repo)
        {
            _multiplexer = multiplexer;
            _next = next;
            _factory = downstreamRouteFinder;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var upstreamUrlPath = httpContext.Request.Path.ToString();

            var upstreamQueryString = httpContext.Request.QueryString.ToString();

            var upstreamHost = httpContext.Request.Headers["Host"];

            Logger.LogDebug($"Upstream url path is {upstreamUrlPath}");

            var provider = _factory.Get(Configuration.Data);

            var downstreamRoute = provider.Get(upstreamUrlPath, upstreamQueryString, httpContext.Request.Method, Configuration.Data, upstreamHost);

            if (downstreamRoute.IsError)
            {
                Logger.LogWarning($"{MiddlewareName} setting pipeline errors. IDownstreamRouteFinder returned {downstreamRoute.Errors.ToErrorString()}");

                SetPipelineError(httpContext, downstreamRoute.Errors);
                return;
            }

            var downstreamPathTemplates = string.Join(", ", downstreamRoute.Data.ReRoute.DownstreamReRoute.Select(r => r.DownstreamPathTemplate.Value));
            Logger.LogDebug($"downstream templates are {downstreamPathTemplates}");

            DownstreamContext.Data.TemplatePlaceholderNameAndValues =
                downstreamRoute.Data.TemplatePlaceholderNameAndValues;

            await _multiplexer.Multiplex(httpContext, downstreamRoute.Data.ReRoute, _next);
        }
    }
}
