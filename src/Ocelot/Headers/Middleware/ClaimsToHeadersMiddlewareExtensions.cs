using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware;

namespace Ocelot.Headers.Middleware
{
    public static class ClaimsToHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseClaimsToHeadersMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotMiddleware, ClaimsToHeadersMiddleware>();
        }
    }
}
