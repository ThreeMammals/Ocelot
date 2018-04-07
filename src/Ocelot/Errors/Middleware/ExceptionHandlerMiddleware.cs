﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration.Provider;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Errors.Middleware
{
    /// <summary>
    /// Catches all unhandled exceptions thrown by middleware, logs and returns a 500
    /// </summary>
    public class ExceptionHandlerMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IOcelotConfigurationProvider _provider;
        private readonly IRequestScopedDataRepository _repo;

        public ExceptionHandlerMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory, 
            IOcelotConfigurationProvider provider, 
            IRequestScopedDataRepository repo)
                : base(loggerFactory.CreateLogger<ExceptionHandlerMiddleware>())
        {
            _provider = provider;
            _repo = repo;
            _next = next;
        }

        public async Task Invoke(DownstreamContext context)
        {
            try
            {               
                await TrySetGlobalRequestId(context);

                Logger.LogDebug("ocelot pipeline started");

                await _next.Invoke(context);
            }
            catch (Exception e)
            {
                Logger.LogDebug("error calling middleware");

                var message = CreateMessage(context, e);

                Logger.LogError(message, e);
                
                SetInternalServerErrorOnResponse(context);
            }

            Logger.LogDebug("ocelot pipeline finished");
        }

        private async Task TrySetGlobalRequestId(DownstreamContext context)
        {
            //try and get the global request id and set it for logs...
            //should this basically be immutable per request...i guess it should!
            //first thing is get config
            var configuration = await _provider.Get(); 
            
            if(configuration.IsError)
            {
                throw new Exception($"{MiddlewareName} setting pipeline errors. IOcelotConfigurationProvider returned {configuration.Errors.ToErrorString()}");
            }

            var key = configuration.Data.RequestId;

            if (!string.IsNullOrEmpty(key) && context.HttpContext.Request.Headers.TryGetValue(key, out var upstreamRequestIds))
            {
                context.HttpContext.TraceIdentifier = upstreamRequestIds.First();
            }

            _repo.Add("RequestId", context.HttpContext.TraceIdentifier);
        }

        private void SetInternalServerErrorOnResponse(DownstreamContext context)
        {
            if (!context.HttpContext.Response.HasStarted)
            {
                context.HttpContext.Response.StatusCode = 500;
            }
        }

        private string CreateMessage(DownstreamContext context, Exception e)
        {
            var message =
                $"Exception caught in global error handler, exception message: {e.Message}, exception stack: {e.StackTrace}";

            if (e.InnerException != null)
            {
                message =
                    $"{message}, inner exception message {e.InnerException.Message}, inner exception stack {e.InnerException.StackTrace}";
            }

            return $"{message} RequestId: {context.HttpContext.TraceIdentifier}";
        }
    }
}
