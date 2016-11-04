using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
                _logger.LogDebug("calling middleware");

                _requestScopedDataRepository.Add("RequestId", context.TraceIdentifier);

                await _next.Invoke(context);

                _logger.LogDebug("succesfully called middleware");
            }
            catch (Exception e)
            {
                _logger.LogDebug("error calling middleware");

                var message = CreateMessage(context, e);
                _logger.LogError(message, e);
                await SetInternalServerErrorOnResponse(context);
            }
        }

        private static async Task SetInternalServerErrorOnResponse(HttpContext context)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("Internal Server Error");
        }

        private static string CreateMessage(HttpContext context, Exception e)
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
