using System.Threading.Tasks;
using System.Linq;
using Ocelot.Configuration.Repository;
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
        private readonly IInternalConfigurationRepository _repo;
        private readonly IMultiplexer _multiplexer;

        public DownstreamRouteFinderMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamRouteFinder downstreamRouteFinder,
            IInternalConfigurationRepository repo,
            IMultiplexer multiplexer)
                :base(loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>())
        {
            _repo = repo;
            _multiplexer = multiplexer;
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var upstreamUrlPath = context.HttpContext.Request.Path.ToString();

            var upstreamHost = context.HttpContext.Request.Headers["Host"];

            var configuration = _repo.Get();

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
            
            var downstreamPathTemplates = string.Join(", ", downstreamRoute.Data.ReRoute.DownstreamReRoute.Select(r => r.DownstreamPathTemplate.Value));
            Logger.LogDebug($"downstream templates are {downstreamPathTemplates}");

            context.TemplatePlaceholderNameAndValues = downstreamRoute.Data.TemplatePlaceholderNameAndValues;

            await _multiplexer.Multiplex(context, downstreamRoute.Data.ReRoute, _next);
        }
    }
}
