using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Builder;
using Ocelot.Requester.QoS;

namespace Ocelot.Request.Middleware
{
    public class HttpRequestBuilderMiddleware : OcelotMiddlewareV2
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IRequestCreator _requestCreator;
        private readonly IOcelotLogger _logger;
        private readonly IQosProviderHouse _qosProviderHouse;

        public HttpRequestBuilderMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestCreator requestCreator, 
            IQosProviderHouse qosProviderHouse)
        {
            _next = next;
            _requestCreator = requestCreator;
            _qosProviderHouse = qosProviderHouse;
            _logger = loggerFactory.CreateLogger<HttpRequestBuilderMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            var qosProvider = _qosProviderHouse.Get(context.DownstreamReRoute);

            if (qosProvider.IsError)
            {
                _logger.LogDebug("IQosProviderHouse returned an error, setting pipeline error");

                SetPipelineError(context, qosProvider.Errors);
                return;
            }

            var buildResult = await _requestCreator.Build(
                    context.DownstreamRequest,
                    context.DownstreamReRoute.IsQos,
                    qosProvider.Data,
                    context.DownstreamReRoute.HttpHandlerOptions.UseCookieContainer,
                    context.DownstreamReRoute.HttpHandlerOptions.AllowAutoRedirect,
                    context.DownstreamReRoute.ReRouteKey,
                    context.DownstreamReRoute.HttpHandlerOptions.UseTracing);
                    
            if (buildResult.IsError)
            {
                _logger.LogDebug("IRequestCreator returned an error, setting pipeline error");
                SetPipelineError(context, buildResult.Errors);
                return;
            }

            _logger.LogDebug("setting upstream request");

            SetUpstreamRequestForThisRequest(context, buildResult.Data);

            await _next.Invoke(context);
        }

        private void SetUpstreamRequestForThisRequest(DownstreamContext context, Request request)
        {
            context.Request = request;
        }
    }
}
