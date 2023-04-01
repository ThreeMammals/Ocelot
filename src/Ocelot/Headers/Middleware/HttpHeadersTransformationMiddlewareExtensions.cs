namespace Ocelot.Headers.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class HttpHeadersTransformationMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpHeadersTransformationMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotHttpHeadersTransformationMiddleware, HttpHeadersTransformationMiddleware>();
        }
    }
}
