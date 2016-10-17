namespace Ocelot.Library.Middleware
{
    using Microsoft.AspNetCore.Builder;

    public static class DownstreamUrlCreatorMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownstreamUrlCreatorMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DownstreamUrlCreatorMiddleware>();
        }
    }
}