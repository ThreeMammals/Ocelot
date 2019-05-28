using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Errors.Middleware
{
    public static class ExceptionHandlerMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseExceptionHandlerMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}
