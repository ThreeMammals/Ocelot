using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Builder;

namespace Ocelot.Request.Middleware
{
    public class HttpRequestBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestCreator _requestCreator;
        private readonly IOcelotLogger _logger;

        public HttpRequestBuilderMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IRequestCreator requestCreator)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requestCreator = requestCreator;
            _logger = loggerFactory.CreateLogger<HttpRequestBuilderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling request builder middleware");

            var buildResult = await _requestCreator
                .Build(context.Request.Method, DownstreamUrl, context.Request.Body,
                    context.Request.Headers, context.Request.Cookies, context.Request.QueryString,
                    context.Request.ContentType, new RequestId.RequestId(DownstreamRoute?.ReRoute?.RequestIdKey, context.TraceIdentifier));

            if (buildResult.IsError)
            {
                _logger.LogDebug("IRequestCreator returned an error, setting pipeline error");

                SetPipelineError(buildResult.Errors);
                return;
            }
            _logger.LogDebug("setting upstream request");

            SetUpstreamRequestForThisRequest(buildResult.Data);

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}