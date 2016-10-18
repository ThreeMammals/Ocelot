using Ocelot.Library.Authentication.Middleware;
using Ocelot.Library.DownstreamRouteFinder.Middleware;
using Ocelot.Library.DownstreamUrlCreator.Middleware;
using Ocelot.Library.HeaderBuilder.Middleware;
using Ocelot.Library.RequestBuilder.Middleware;
using Ocelot.Library.Requester.Middleware;
using Ocelot.Library.Responder.Middleware;

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

            builder.UseHttpRequestHeadersBuilderMiddleware();

            builder.UseDownstreamUrlCreatorMiddleware();

            builder.UseHttpRequestBuilderMiddleware();

            builder.UseHttpRequesterMiddleware();

            return builder;
        }
    }
}
