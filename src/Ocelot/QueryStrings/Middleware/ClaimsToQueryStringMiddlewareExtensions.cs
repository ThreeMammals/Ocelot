namespace Ocelot.QueryStrings.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class ClaimsToQueryStringMiddlewareExtensions
    {
        public static IApplicationBuilder UseClaimsToQueryStringMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotClaimsToQueryStringMiddleware, ClaimsToQueryStringMiddleware>();
        }
    }
}
