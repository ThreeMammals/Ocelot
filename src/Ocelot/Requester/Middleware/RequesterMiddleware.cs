using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Requester.Handler;
using Ocelot.Requester.Handler.Factory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Requester.Middleware
{
    public class RequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _app;
        private readonly IRequesterHandlerFactory _requesterHandlerFactory;
        private readonly IOcelotLogger _logger;

        public RequesterMiddleware(RequestDelegate next,
            IApplicationBuilder app,
            IRequestScopedDataRepository requestScopedDataRepository,
            IRequesterHandlerFactory requesterHandlerFactory,
            IOcelotLoggerFactory loggerFactory)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _requesterHandlerFactory = requesterHandlerFactory;
            _app = app;
            _logger = loggerFactory.CreateLogger<RequesterMiddleware>();
        }


        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling requester middleware");

            var requesterHandler = _requesterHandlerFactory.Get(_app, DownstreamRoute.ReRoute.DownstreamScheme);

            if (requesterHandler.IsError)
            {
                _logger.LogError($"Error getting requester handler for {context.Request.Path}. {requesterHandler.Errors.ToErrorString()}");
                SetPipelineError(requesterHandler.Errors);
                return;
            }

            await requesterHandler.Data.Handler.Handle(context);

            _logger.LogDebug($"Client has been requestered for {context.Request.Path}");

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            _logger.LogDebug("succesfully called next middleware");

        }
       
    }
}
