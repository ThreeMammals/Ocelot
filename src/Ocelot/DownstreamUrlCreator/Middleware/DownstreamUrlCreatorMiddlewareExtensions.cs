namespace Ocelot.DownstreamUrlCreator.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class DownstreamUrlCreatorMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownstreamUrlCreatorMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotDownstreamUrlCreatorMiddleware, DownstreamUrlCreatorMiddleware>();
        }
    }
}
