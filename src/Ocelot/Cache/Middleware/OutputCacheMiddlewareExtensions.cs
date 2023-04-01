namespace Ocelot.Cache.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class OutputCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseOutputCacheMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotOutputCacheMiddleware, OutputCacheMiddleware>();
        }
    }
}
