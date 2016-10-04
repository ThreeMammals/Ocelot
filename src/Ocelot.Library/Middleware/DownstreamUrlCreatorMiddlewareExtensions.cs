using Microsoft.AspNetCore.Builder;

namespace Ocelot.Library.Middleware
{
    public static class DownstreamUrlCreatorMiddlewareExtensions
    {
        public static IApplicationBuilder UserDownstreamUrlCreatorMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DownstreamUrlCreatorMiddleware>();
        }
    }
}