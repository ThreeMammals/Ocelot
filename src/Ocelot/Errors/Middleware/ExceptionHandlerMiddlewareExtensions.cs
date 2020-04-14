using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Errors.Middleware
{
    public static class ExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}
