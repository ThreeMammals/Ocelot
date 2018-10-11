namespace Ocelot.QueryStrings.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware.Pipeline;

    public static class ClaimsToQueryStringMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseClaimsToQueryStringMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ClaimsToQueryStringMiddleware>();
        }
    }
}
