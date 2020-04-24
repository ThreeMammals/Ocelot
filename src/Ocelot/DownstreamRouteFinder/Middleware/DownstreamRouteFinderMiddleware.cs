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

    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteProviderFactory _factory;
        private readonly IMultiplexer _multiplexer;

        public DownstreamRouteFinderMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamRouteProviderFactory downstreamRouteFinder,
            IMultiplexer multiplexer
            )
                : base(loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>())
        {
            _multiplexer = multiplexer;
            _next = next;
            _factory = downstreamRouteFinder;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            var upstreamUrlPath = httpContext.Request.Path.ToString();

            var upstreamQueryString = httpContext.Request.QueryString.ToString();

            var upstreamHost = httpContext.Request.Headers["Host"];

            Logger.LogDebug($"Upstream url path is {upstreamUrlPath}");

            var provider = _factory.Get(downstreamContext.Configuration);

            var response = provider.Get(upstreamUrlPath, upstreamQueryString, httpContext.Request.Method, downstreamContext.Configuration, upstreamHost);

            if (response.IsError)
            {
                Logger.LogWarning($"{MiddlewareName} setting pipeline errors. IDownstreamRouteFinder returned {response.Errors.ToErrorString()}");

                SetPipelineError(downstreamContext, response.Errors);
                return;
            }

            var downstreamPathTemplates = string.Join(", ", response.Data.ReRoute.DownstreamReRoute.Select(r => r.DownstreamPathTemplate.Value));
            Logger.LogDebug($"downstream templates are {downstreamPathTemplates}");

            downstreamContext.TemplatePlaceholderNameAndValues =
                response.Data.TemplatePlaceholderNameAndValues;

            downstreamContext.DownstreamRoute = response.Data;

            await _next.Invoke(httpContext);
            //await _multiplexer.Multiplex(downstreamContext, httpContext, _next);
        }
    }
}
