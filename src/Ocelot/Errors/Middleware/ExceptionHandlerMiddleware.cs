using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.Errors.Middleware
{
    public class ExceptionHandlerMiddleware 
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public ExceptionHandlerMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory, 
            IRequestScopedDataRepository requestScopedDataRepository)
        {
            _next = next;
            _requestScopedDataRepository = requestScopedDataRepository;
            _logger = loggerFactory.CreateLogger<ExceptionHandlerMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                _logger.LogDebug("ocelot pipeline started");

                _logger.LogDebug("calling next middleware");

                await _next.Invoke(context);

                _logger.LogDebug("succesfully called middleware");
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

        private void SetInternalServerErrorOnResponse(HttpContext context)
        {
            context.Response.OnStarting(x =>
            {
                context.Response.StatusCode = 500;
                return Task.CompletedTask;
            }, context);
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
