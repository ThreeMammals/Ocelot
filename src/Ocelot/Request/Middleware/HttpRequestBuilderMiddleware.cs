using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Builder;
using Ocelot.Requester.QoS;

namespace Ocelot.Request.Middleware
{
    public class HttpRequestBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestCreator _requestCreator;
        private readonly IOcelotLogger _logger;
        private readonly IQosProviderHouse _qosProviderHouse;

        public HttpRequestBuilderMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IRequestCreator requestCreator, 
            IQosProviderHouse qosProviderHouse)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requestCreator = requestCreator;
            _qosProviderHouse = qosProviderHouse;
            _logger = loggerFactory.CreateLogger<HttpRequestBuilderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var qosProvider = _qosProviderHouse.Get(DownstreamRoute.ReRoute);

            if (qosProvider.IsError)
            {
                _logger.LogDebug("IQosProviderHouse returned an error, setting pipeline error");

                SetPipelineError(qosProvider.Errors);

                return;
            }

            var buildResult = await _requestCreator.Build(
                    DownstreamRequest,
                    DownstreamRoute.ReRoute.IsQos,
                    qosProvider.Data,
                    DownstreamRoute.ReRoute.HttpHandlerOptions.UseCookieContainer,
                    DownstreamRoute.ReRoute.HttpHandlerOptions.AllowAutoRedirect,
                    DownstreamRoute.ReRoute.ReRouteKey,
                    DownstreamRoute.ReRoute.HttpHandlerOptions.UseTracing);
                    
            if (buildResult.IsError)
            {
                _logger.LogDebug("IRequestCreator returned an error, setting pipeline error");
                SetPipelineError(buildResult.Errors);
                return;
            }

            _logger.LogDebug("setting upstream request");

            SetUpstreamRequestForThisRequest(buildResult.Data);

            await _next.Invoke(context);
        }
    }
}
