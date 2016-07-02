using Microsoft.AspNetCore.Builder;

namespace Ocelot.ApiGateway.Middleware
{
    public static class RequestLoggerExtensions
    {
        public static IApplicationBuilder UseRequestLogger(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggerMiddleware>();
        }
    }
} 