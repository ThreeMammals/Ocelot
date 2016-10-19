using Microsoft.AspNetCore.Builder;
using Ocelot.Authentication.Middleware;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.HeaderBuilder.Middleware;
using Ocelot.RequestBuilder.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.Responder.Middleware;

namespace Ocelot.Middleware
{
    using Authorisation.Middleware;
    using ClaimsBuilder.Middleware;

    public static class OcelotMiddlewareExtensions
    {
        public static IApplicationBuilder UseOcelot(this IApplicationBuilder builder)
        {
            builder.UseHttpResponderMiddleware();

            builder.UseDownstreamRouteFinderMiddleware();

            builder.UseAuthenticationMiddleware();

            builder.UseClaimsBuilderMiddleware();

            builder.UseAuthorisationMiddleware();

            builder.UseHttpRequestHeadersBuilderMiddleware();

            builder.UseDownstreamUrlCreatorMiddleware();

            builder.UseHttpRequestBuilderMiddleware();

            builder.UseHttpRequesterMiddleware();

            return builder;
        }
    }
}
