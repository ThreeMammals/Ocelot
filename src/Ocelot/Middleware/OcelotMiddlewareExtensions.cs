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
    using System;
    using System.Threading.Tasks;
    using Authorisation.Middleware;
    using ClaimsBuilder.Middleware;
    using Microsoft.AspNetCore.Http;

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

        public static IApplicationBuilder UseOcelot(this IApplicationBuilder builder, OcelotMiddlewareConfiguration middlewareConfiguration)
        {
            builder.UseIfNotNull(middlewareConfiguration.PreHttpResponderMiddleware);

            builder.UseHttpResponderMiddleware();

            builder.UseIfNotNull(middlewareConfiguration.PostHttpResponderMiddleware);

            builder.UseIfNotNull(middlewareConfiguration.PreDownstreamRouteFinderMiddleware);

            builder.UseDownstreamRouteFinderMiddleware();

            builder.UseIfNotNull(middlewareConfiguration.PostDownstreamRouteFinderMiddleware);

            builder.UseIfNotNull(middlewareConfiguration.PreAuthenticationMiddleware);

            builder.UseAuthenticationMiddleware();

            builder.UseIfNotNull(middlewareConfiguration.PostAuthenticationMiddleware);

            builder.UseIfNotNull(middlewareConfiguration.PreClaimsBuilderMiddleware);

            builder.UseClaimsBuilderMiddleware();

            builder.UseIfNotNull(middlewareConfiguration.PostClaimsBuilderMiddleware);

            builder.UseIfNotNull(middlewareConfiguration.PreAuthorisationMiddleware);

            builder.UseAuthorisationMiddleware();

            builder.UseIfNotNull(middlewareConfiguration.PostAuthorisationMiddleware);

            builder.UseIfNotNull(middlewareConfiguration.PreHttpRequestHeadersBuilderMiddleware);

            builder.UseHttpRequestHeadersBuilderMiddleware();

            builder.UseIfNotNull(middlewareConfiguration.PostHttpRequestHeadersBuilderMiddleware);

            builder.UseIfNotNull(middlewareConfiguration.PreDownstreamUrlCreatorMiddleware);

            builder.UseDownstreamUrlCreatorMiddleware();

            builder.UseIfNotNull(middlewareConfiguration.PostDownstreamUrlCreatorMiddleware);

            builder.UseIfNotNull(middlewareConfiguration.PreHttpRequestBuilderMiddleware);

            builder.UseHttpRequestBuilderMiddleware();

            builder.UseIfNotNull(middlewareConfiguration.PostHttpRequestBuilderMiddleware);

            builder.UseIfNotNull(middlewareConfiguration.PreHttpRequesterMiddleware);

            builder.UseHttpRequesterMiddleware();

            return builder;
        }

        private static void UseIfNotNull(this IApplicationBuilder builder, Func<HttpContext, Func<Task>, Task> middleware)
        {
            if (middleware != null)
            {
                builder.Use(middleware);
            }
        }
    }
}
