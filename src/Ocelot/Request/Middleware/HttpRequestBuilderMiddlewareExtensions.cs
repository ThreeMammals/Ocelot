namespace Ocelot.Request.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class HttpRequestBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownstreamRequestInitialiser(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotDownstreamRequestInitialiserMiddleware, DownstreamRequestInitialiserMiddleware>();
        }
    }
}
