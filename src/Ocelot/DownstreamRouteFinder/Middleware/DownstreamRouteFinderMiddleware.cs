using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Utilities;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IOcelotLogger _logger;

        public DownstreamRouteFinderMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamRouteFinder downstreamRouteFinder, 
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
            _logger = loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.TraceMiddlewareEntry();

            var upstreamUrlPath = context.Request.Path.ToString().SetLastCharacterAs('/');

            _logger.LogDebug("upstream url path is {upstreamUrlPath}", upstreamUrlPath);

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.Request.Method);

            if (downstreamRoute.IsError)
            {
                _logger.LogError($"{MiddlwareName} setting pipeline errors. IDownstreamRouteFinder returned {downstreamRoute.Errors.ToErrorString()}");

                SetPipelineError(downstreamRoute.Errors);

                _logger.TraceMiddlewareCompleted();
                return;
            }

            _logger.LogDebug("downstream template is {downstreamRoute.Data.ReRoute.DownstreamPath}", downstreamRoute.Data.ReRoute.DownstreamPathTemplate);

            SetDownstreamRouteForThisRequest(downstreamRoute.Data);

            _logger.TraceInvokeNext();

            await _next.Invoke(context);

            _logger.TraceInvokeNextCompleted();
            _logger.TraceMiddlewareCompleted();
        }
    }
}