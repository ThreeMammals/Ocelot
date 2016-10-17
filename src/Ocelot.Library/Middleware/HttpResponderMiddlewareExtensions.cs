namespace Ocelot.Library.Middleware
{
    using Microsoft.AspNetCore.Builder;

    public static class HttpResponderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpResponderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpResponderMiddleware>();
        }
    }
}