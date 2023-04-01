namespace Ocelot.Requester.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class HttpRequesterMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequesterMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotHttpRequesterMiddleware, HttpRequesterMiddleware>();
        }
    }
}
