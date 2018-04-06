using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Configuration.Provider;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IOcelotConfigurationProvider _configProvider;
        private readonly IMultiplexer _multiplexer;

        public DownstreamRouteFinderMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamRouteFinder downstreamRouteFinder,
            IOcelotConfigurationProvider configProvider,
            IMultiplexer multiplexer)
                :base(loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>())
        {
            _configProvider = configProvider;
            _multiplexer = multiplexer;
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var upstreamUrlPath = context.HttpContext.Request.Path.ToString();

            var upstreamHost = context.HttpContext.Request.Headers["Host"];

            var configuration = await _configProvider.Get();

            if (configuration.IsError)
            {
                Logger.LogWarning($"{MiddlewareName} setting pipeline errors. IOcelotConfigurationProvider returned {configuration.Errors.ToErrorString()}");
                SetPipelineError(context, configuration.Errors);
                return;
            }

            context.ServiceProviderConfiguration = configuration.Data.ServiceProviderConfiguration;

            Logger.LogDebug($"Upstream url path is {upstreamUrlPath}");

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.HttpContext.Request.Method, configuration.Data, upstreamHost);

            if (downstreamRoute.IsError)
            {
                Logger.LogWarning($"{MiddlewareName} setting pipeline errors. IDownstreamRouteFinder returned {downstreamRoute.Errors.ToErrorString()}");

                SetPipelineError(context, downstreamRoute.Errors);
                return;
            }

            // todo - put this back in
            //// _logger.LogDebug("downstream template is {downstreamRoute.Data.ReRoute.DownstreamPath}", downstreamRoute.Data.ReRoute.DownstreamReRoute.DownstreamPathTemplate);

            context.TemplatePlaceholderNameAndValues = downstreamRoute.Data.TemplatePlaceholderNameAndValues;

            await _multiplexer.Multiplex(context, downstreamRoute.Data.ReRoute, _next);
        }
    }
}
