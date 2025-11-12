using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Authorization;

public class AuthorizationMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClaimsAuthorizer _claimsAuthorizer;
    private readonly IScopesAuthorizer _scopesAuthorizer;

    public AuthorizationMiddleware(RequestDelegate next,
        IClaimsAuthorizer claimsAuthorizer,
        IScopesAuthorizer scopesAuthorizer,
        IOcelotLoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<AuthorizationMiddleware>())
    {
        _next = next;
        _claimsAuthorizer = claimsAuthorizer;
        _scopesAuthorizer = scopesAuthorizer;
    }

    public async Task Invoke(HttpContext context)
    {
        var route = context.Items.DownstreamRoute();

        if (!IsOptionsHttpMethod(context) && route.IsAuthenticated)
        {
            Logger.LogInformation("route is authenticated scopes must be checked");

            var authorized = _scopesAuthorizer.Authorize(context.User, route.AuthenticationOptions.AllowedScopes);

            if (authorized.IsError)
            {
                Logger.LogWarning("error authorizing user scopes");

                context.Items.UpsertErrors(authorized.Errors);
                return;
            }

            if (IsAuthorized(authorized))
            {
                Logger.LogInformation("user scopes is authorized calling next authorization checks");
            }
            else
            {
                Logger.LogWarning("user scopes is not authorized setting pipeline error");

                context.Items.SetError(new UnauthorizedError(
                        $"{context.User.Identity.Name} unable to access {route.UpstreamPathTemplate.OriginalValue}"));
            }
        }

        if (!IsOptionsHttpMethod(context) && route.IsAuthorized)
        {
            Logger.LogInformation("route is authorized");

            var authorized = _claimsAuthorizer.Authorize(context.User, route.RouteClaimsRequirement, context.Items.TemplatePlaceholderNameAndValues());

            if (authorized.IsError)
            {
                Logger.LogWarning(() => $"Error whilst authorizing {context.User.Identity.Name}. Setting pipeline error");

                context.Items.UpsertErrors(authorized.Errors);
                return;
            }

            if (IsAuthorized(authorized))
            {
                Logger.LogInformation(() => $"{context.User.Identity.Name} has succesfully been authorized for {route.UpstreamPathTemplate.OriginalValue}.");
                await _next.Invoke(context);
            }
            else
            {
                Logger.LogWarning(() => $"{context.User.Identity.Name} is not authorized to access {route.UpstreamPathTemplate.OriginalValue}. Setting pipeline error");

                context.Items.SetError(new UnauthorizedError($"{context.User.Identity.Name} is not authorized to access {route.UpstreamPathTemplate.OriginalValue}"));
            }
        }
        else
        {
            Logger.LogInformation(() => $"No authorization needed for upstream path: { route.UpstreamPathTemplate.OriginalValue}");
            await _next.Invoke(context);
        }
    }

    private static bool IsAuthorized(Response<bool> authorized)
    {
        return authorized.Data;
    }

    private static bool IsOptionsHttpMethod(HttpContext httpContext)
    {
        return httpContext.Request.Method.ToUpper() == "OPTIONS";
    }
}
