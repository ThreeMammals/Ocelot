using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration.Provider;
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
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;
        private readonly IOcelotConfigurationProvider _configProvider;


        public ExceptionHandlerMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory, 
            IRequestScopedDataRepository requestScopedDataRepository,
            IOcelotConfigurationProvider configProvider)
            :base(requestScopedDataRepository)
        {
            _configProvider = configProvider;
            _next = next;
            _requestScopedDataRepository = requestScopedDataRepository;
            _logger = loggerFactory.CreateLogger<ExceptionHandlerMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {               
                await TrySetGlobalRequestId(context);

                _logger.LogDebug("ocelot pipeline started");

                await _next.Invoke(context);

            }
            catch (Exception e)
            {
                _logger.LogDebug("error calling middleware");

                var message = CreateMessage(context, e);

                _logger.LogError(message, e);
                
                SetInternalServerErrorOnResponse(context);
            }

            _logger.LogDebug("ocelot pipeline finished");
        }

        private async Task TrySetGlobalRequestId(HttpContext context)
        {
                //try and get the global request id and set it for logs...
                //should this basically be immutable per request...i guess it should!
                //first thing is get config
                 var configuration = await _configProvider.Get(); 
            
                //if error throw to catch below..
                if(configuration.IsError)
                {
                    throw new Exception($"{MiddlewareName} setting pipeline errors. IOcelotConfigurationProvider returned {configuration.Errors.ToErrorString()}");
                }

                //else set the request id?
                var key = configuration.Data.RequestId;

                StringValues upstreamRequestIds;
                if (!string.IsNullOrEmpty(key) && context.Request.Headers.TryGetValue(key, out upstreamRequestIds))
                {
                    context.TraceIdentifier = upstreamRequestIds.First();
                    _requestScopedDataRepository.Add<string>("RequestId", context.TraceIdentifier);
                }
        }

        private void SetInternalServerErrorOnResponse(HttpContext context)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
            }
        }

        private string CreateMessage(HttpContext context, Exception e)
        {
            var message =
                $"Exception caught in global error handler, exception message: {e.Message}, exception stack: {e.StackTrace}";

            if (e.InnerException != null)
            {
                message =
                    $"{message}, inner exception message {e.InnerException.Message}, inner exception stack {e.InnerException.StackTrace}";
            }
            return $"{message} RequestId: {context.TraceIdentifier}";
        }
    }
}
