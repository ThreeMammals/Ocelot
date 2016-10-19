namespace Ocelot.ClaimsBuilder.Middleware
{
    using Microsoft.AspNetCore.Builder;

    public static class ClaimsBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseClaimsBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClaimsBuilderMiddleware>();
        }
    }
}