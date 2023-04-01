namespace Ocelot.Responder.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class ResponderMiddlewareExtensions
    {
        public static IApplicationBuilder UseResponderMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotResponderMiddleware, ResponderMiddleware>();
        }
    }
}
