using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Configuration.Provider;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IOcelotLogger _logger;
        private readonly IOcelotConfigurationProvider _configProvider;


        public DownstreamRouteFinderMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamRouteFinder downstreamRouteFinder,
            IOcelotConfigurationProvider configProvider)
        {
            _configProvider = configProvider;
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
            _logger = loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            var upstreamUrlPath = context.HttpContext.Request.Path.ToString();

            var upstreamHost = context.HttpContext.Request.Headers["Host"];

            var configuration = await _configProvider.Get();

            if (configuration.IsError)
            {
                _logger.LogError($"{MiddlewareName} setting pipeline errors. IOcelotConfigurationProvider returned {configuration.Errors.ToErrorString()}");
                SetPipelineError(context, configuration.Errors);
                return;
            }

            context.ServiceProviderConfiguration = configuration.Data.ServiceProviderConfiguration;

            _logger.LogDebug("upstream url path is {upstreamUrlPath}", upstreamUrlPath);

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.HttpContext.Request.Method, configuration.Data, upstreamHost);

            if (downstreamRoute.IsError)
            {
                _logger.LogError($"{MiddlewareName} setting pipeline errors. IDownstreamRouteFinder returned {downstreamRoute.Errors.ToErrorString()}");

                SetPipelineError(context, downstreamRoute.Errors);
                return;
            }

            //todo - put this back in
            // _logger.LogDebug("downstream template is {downstreamRoute.Data.ReRoute.DownstreamPath}", downstreamRoute.Data.ReRoute.DownstreamReRoute.DownstreamPathTemplate);

            context.TemplatePlaceholderNameAndValues = downstreamRoute.Data.TemplatePlaceholderNameAndValues;

            await Multiplex(context, downstreamRoute.Data.ReRoute);
        }

        private async Task Multiplex(DownstreamContext context, ReRoute reRoute)
        {
            var tasks = new Task<DownstreamContext>[reRoute.DownstreamReRoute.Count];
            for (int i = 0; i < reRoute.DownstreamReRoute.Count; i++)
            {
                var downstreamContext = new DownstreamContext(context.HttpContext)
                {
                    TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
                    ServiceProviderConfiguration = context.ServiceProviderConfiguration,
                    DownstreamReRoute = reRoute.DownstreamReRoute[i],
                    //todo do we want these set here
                    RequestId = context.RequestId,
                    PreviousRequestId = context.PreviousRequestId,
                };

                tasks[i] = Fire(downstreamContext);
            }

            await Task.WhenAll(tasks);

            //now cast the complete tasks to whatever they need to be
            //store them and let the response middleware handle them..

            var finished = tasks[0].Result;

            context.Errors = finished.Errors;
            context.DownstreamRequest = finished.DownstreamRequest;
            context.DownstreamResponse = finished.DownstreamResponse;
            context.RequestId = finished.RequestId;
            context.PreviousRequestId = finished.RequestId;
            
        }

        private async Task<DownstreamContext> Fire(DownstreamContext context)
        {
            await _next.Invoke(context);
            return context;
        }
    }
}
