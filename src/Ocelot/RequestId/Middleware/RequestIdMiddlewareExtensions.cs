namespace Ocelot.RequestId.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class RequestIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestIdMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotRequestIdMiddleware, RequestIdMiddleware>();
        }
    }
}
