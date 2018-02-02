using Microsoft.AspNetCore.Builder;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public static class DownstreamUrlCreatorMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownstreamUrlCreatorMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DownstreamUrlCreatorMiddleware>();
        }
    }
}