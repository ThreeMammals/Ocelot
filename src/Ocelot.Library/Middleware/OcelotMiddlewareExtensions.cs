namespace Ocelot.Library.Middleware
{
    using Microsoft.AspNetCore.Builder;

    public static class OcelotMiddlewareExtensions
    {
        public static IApplicationBuilder UseOcelot(this IApplicationBuilder builder)
        {
            builder.UseHttpResponderMiddleware();

            builder.UseDownstreamRouteFinderMiddleware();

            builder.UseAuthenticationMiddleware();

            builder.UseDownstreamUrlCreatorMiddleware();

            builder.UseHttpRequestBuilderMiddleware();

            builder.UseHttpRequesterMiddleware();

            return builder;
        }
    }
}
