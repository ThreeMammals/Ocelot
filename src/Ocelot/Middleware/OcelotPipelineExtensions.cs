using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Authentication.Middleware;
using Ocelot.Authorization.Middleware;
using Ocelot.Cache;
using Ocelot.Claims.Middleware;
using Ocelot.DownstreamPathManipulation.Middleware;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Errors.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.LoadBalancer;
using Ocelot.Multiplexer;
using Ocelot.QueryStrings.Middleware;
using Ocelot.RateLimiting;
using Ocelot.Request.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Responder.Middleware;
using Ocelot.Security.Middleware;
using Ocelot.WebSockets;

namespace Ocelot.Middleware;

public static class OcelotPipelineExtensions
{
    public static RequestDelegate BuildOcelotPipeline(this IApplicationBuilder app, OcelotPipelineConfiguration configuration)
    {
        // this sets up the downstream context and gets the config
        app.UseMiddleware<ConfigurationMiddleware>();

        // This is registered to catch any global exceptions that are not handled
        // It also sets the Request Id if anything is set globally
        app.UseMiddleware<ExceptionHandlerMiddleware>();

        // If the request is for websockets upgrade we fork into a different pipeline
        app.MapWhen(httpContext => httpContext.WebSockets.IsWebSocketRequest,
            ws =>
            {
                ws.UseMiddleware<DownstreamRouteFinderMiddleware>();
                ws.UseMiddleware<MultiplexingMiddleware>();
                ws.UseMiddleware<DownstreamRequestInitialiserMiddleware>();
                ws.UseMiddleware<LoadBalancingMiddleware>();
                ws.UseMiddleware<DownstreamUrlCreatorMiddleware>();
                ws.UseMiddleware<WebSocketsProxyMiddleware>();
            });

        // Allow the user to respond with absolutely anything they want.
        app.UseIfNotNull(configuration.PreErrorResponderMiddleware);

        // This is registered first so it can catch any errors and issue an appropriate response
        app.UseIfNotNull<ResponderMiddleware>(configuration.ResponderMiddleware);

        // Then we get the downstream route information
        app.UseMiddleware<DownstreamRouteFinderMiddleware>();

        // Multiplex the request if required
        app.UseMiddleware<MultiplexingMiddleware>();

        // This security module, IP whitelist blacklist, extended security mechanism
        app.UseMiddleware<SecurityMiddleware>();

        //Expand other branch pipes
        if (configuration.MapWhenOcelotPipeline != null)
        {
            foreach (var pipeline in configuration.MapWhenOcelotPipeline)
            {
                // todo why is this asking for an app app?
                app.MapWhen(pipeline.Key, pipeline.Value);
            }
        }

        // Now we have the ds route we can transform headers and stuff?
        app.UseMiddleware<HttpHeadersTransformationMiddleware>();

        // Initialises downstream request
        app.UseMiddleware<DownstreamRequestInitialiserMiddleware>();

        // We check whether the request is ratelimit, and if there is no continue processing
        app.UseMiddleware<RateLimitingMiddleware>();

        // This adds or updates the request id (initally we try and set this based on global config in the error handling middleware)
        // If anything was set at global level and we have a different setting at re route level the global stuff will be overwritten
        // This means you can get a scenario where you have a different request id from the first piece of middleware to the request id middleware.
        app.UseMiddleware<RequestIdMiddleware>();

        // Allow pre authentication logic. The idea being people might want to run something custom before what is built in.
        app.UseIfNotNull(configuration.PreAuthenticationMiddleware);

        // Now we know where the client is going to go we can authenticate them.
        // We allow the Ocelot middleware to be overriden by whatever the user wants.
        app.UseIfNotNull<AuthenticationMiddleware>(configuration.AuthenticationMiddleware);

        // The next thing we do is look at any claims transforms in case this is important for authorization
        app.UseMiddleware<ClaimsToClaimsMiddleware>();

        // Allow pre authorization logic. The idea being people might want to run something custom before what is built in.
        app.UseIfNotNull(configuration.PreAuthorizationMiddleware);

        // Now we have authenticated and done any claims transformation, we can authorize the request by AuthorizationMiddleware.
        // We allow the Ocelot middleware to be overriden by whatever the user wants.
        app.UseIfNotNull<AuthorizationMiddleware>(configuration.AuthorizationMiddleware);

        // Now we can run the ClaimsToHeadersMiddleware: we allow the Ocelot middleware to be overriden by whatever the user wants.
        app.UseIfNotNull<ClaimsToHeadersMiddleware>(configuration.ClaimsToHeadersMiddleware);

        // Allow the user to implement their own query string manipulation logic
        app.UseIfNotNull(configuration.PreQueryStringBuilderMiddleware);

        // Now we can run any claims to query string transformation middleware
        app.UseMiddleware<ClaimsToQueryStringMiddleware>();

        app.UseMiddleware<ClaimsToDownstreamPathMiddleware>();

        // Get the load balancer for this request
        app.UseMiddleware<LoadBalancingMiddleware>();

        // This takes the downstream route we retrieved earlier and replaces any placeholders with the variables that should be used
        app.UseMiddleware<DownstreamUrlCreatorMiddleware>();

        // Not sure if this is the best place for this but we use the downstream url
        // as the basis for our cache key.
        app.UseMiddleware<OutputCacheMiddleware>();

        //We fire off the request and set the response on the scoped data repo
        app.UseMiddleware<HttpRequesterMiddleware>();

        return app.Build();
    }

    private static IApplicationBuilder UseIfNotNull(this IApplicationBuilder builder, Func<HttpContext, Func<Task>, Task> middleware)
        => middleware != null ? builder.Use(middleware) : builder;

    private static IApplicationBuilder UseIfNotNull<TMiddleware>(this IApplicationBuilder builder, Func<HttpContext, Func<Task>, Task> middleware)
        where TMiddleware : OcelotMiddleware => middleware != null
            ? builder.Use(middleware)
            : builder.UseMiddleware<TMiddleware>();
}
