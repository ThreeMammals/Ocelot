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
            // This is registered to catch any global exceptions that are not handled
            builder.UseExceptionHandlerMiddleware();

            // This is registered first so it can catch any errors and issue an appropriate response
            builder.UseHttpErrorResponderMiddleware();

            // Then we get the downstream route information
            builder.UseDownstreamRouteFinderMiddleware();

            // Now we know where the client is going to go we can authenticate them
            builder.UseAuthenticationMiddleware();

            // The next thing we do is look at any claims transforms in case this is important for authorisation
            builder.UseClaimsBuilderMiddleware();

            // Now we have authenticated and done any claims transformation we can authorise the request
            builder.UseAuthorisationMiddleware();

            // Now we can run any header transformation logic
            builder.UseHttpRequestHeadersBuilderMiddleware();

            // This takes the downstream route we retrieved earlier and replaces any placeholders with the variables that should be used
            builder.UseDownstreamUrlCreatorMiddleware();

            // Everything should now be ready to build or HttpRequest
            builder.UseHttpRequestBuilderMiddleware();

            //We fire off the request and set the response on the context in this middleware
            builder.UseHttpRequesterMiddleware();

            return builder;
        }

        public static IApplicationBuilder UseOcelot(this IApplicationBuilder builder, OcelotMiddlewareConfiguration middlewareConfiguration)
        {
            builder.UseIfNotNull(middlewareConfiguration.PreHttpResponderMiddleware);

            builder.UseHttpErrorResponderMiddleware();

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
