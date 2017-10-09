using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Builder.Factory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Request.Middleware
{
    public class RequestBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _app;
        private readonly IRequestBuilderFactory _requestBuilderFactory;
        private readonly IOcelotLogger _logger;

        public RequestBuilderMiddleware(RequestDelegate next,
            IApplicationBuilder app,
            IRequestScopedDataRepository requestScopedDataRepository,
            IRequestBuilderFactory requestBuilderFactory,
            IOcelotLoggerFactory loggerFactory)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _requestBuilderFactory = requestBuilderFactory;
            _app = app;
            _logger = loggerFactory.CreateLogger<RequestBuilderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling request builder middleware");

            var requesterHandler = _requestBuilderFactory.Get(_app, DownstreamRoute.ReRoute.DownstreamScheme);

            if (requesterHandler.IsError)
            {
                _logger.LogError($"Error getting request builder for {context.Request.Path}. {requesterHandler.Errors.ToErrorString()}");
                SetPipelineError(requesterHandler.Errors);
                _logger.LogDebug("IRequestBuilder returned an error, setting pipeline error");
                return;
            }

            _logger.LogDebug($"setting upstream request");

            await requesterHandler.Data.Handler.Handle(context);

            _logger.LogDebug("calling next middleware");
            await _next.Invoke(context);
    

            _logger.LogDebug("succesfully called next middleware");

        }
    }
}
