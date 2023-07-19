using Microsoft.AspNetCore.Builder;

namespace Ocelot.QueryStrings.Middleware
{
    public static class ClaimsToQueryStringMiddlewareExtensions
    {
        public static IApplicationBuilder UseClaimsToQueryStringMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClaimsToQueryStringMiddleware>();
        }
    }
}
