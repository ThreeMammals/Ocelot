using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Provider;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IOcelotLogger _logger;
        private readonly IOcelotConfigurationProvider _configProvider;


        public DownstreamRouteFinderMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamRouteFinder downstreamRouteFinder, 
            IRequestScopedDataRepository requestScopedDataRepository,
            IOcelotConfigurationProvider configProvider)
            :base(requestScopedDataRepository)
        {
            _configProvider = configProvider;
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
            _logger = loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var upstreamUrlPath = context.Request.Path.ToString();

            var upstreamHost = context.Request.Headers["Host"];

            var configuration = await _configProvider.Get(); 
            
            if(configuration.IsError)
            {
                _logger.LogError($"{MiddlewareName} setting pipeline errors. IOcelotConfigurationProvider returned {configuration.Errors.ToErrorString()}");
                SetPipelineError(configuration.Errors);
                return;
            }

            SetServiceProviderConfigurationForThisRequest(configuration.Data.ServiceProviderConfiguration);

            _logger.LogDebug("upstream url path is {upstreamUrlPath}", upstreamUrlPath);

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.Request.Method, configuration.Data, upstreamHost);

            if (downstreamRoute.IsError)
            {
                _logger.LogError($"{MiddlewareName} setting pipeline errors. IDownstreamRouteFinder returned {downstreamRoute.Errors.ToErrorString()}");

                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            _logger.LogDebug("downstream template is {downstreamRoute.Data.ReRoute.DownstreamPath}", downstreamRoute.Data.ReRoute.DownstreamPathTemplate);

            SetDownstreamRouteForThisRequest(downstreamRoute.Data);

            await _next.Invoke(context);
        }
    }
}
