namespace Ocelot.DownstreamPathManipulation.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class ClaimsToDownstreamPathMiddlewareExtensions
    {
        public static IApplicationBuilder UseClaimsToDownstreamPathMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotMiddleware, ClaimsToDownstreamPathMiddleware>();
        }
    }
}
