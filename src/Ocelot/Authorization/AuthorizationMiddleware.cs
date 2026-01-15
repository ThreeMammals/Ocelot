using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Net.Http;

namespace Ocelot.Authorization;

public class AuthorizationMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClaimsAuthorizer _claimsAuthorizer;
    private readonly IScopesAuthorizer _scopesAuthorizer;
    private readonly IRolesAuthorizer _rolesAuthorizer;

    public AuthorizationMiddleware(RequestDelegate next,
        IClaimsAuthorizer claimsAuthorizer,
        IScopesAuthorizer scopesAuthorizer,
        IRolesAuthorizer rolesAuthorizer,
        IOcelotLoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<AuthorizationMiddleware>())
    {
        _next = next;
        _claimsAuthorizer = claimsAuthorizer;
        _scopesAuthorizer = scopesAuthorizer;
        _rolesAuthorizer = rolesAuthorizer;
    }

    // Note roles is a duplicate of scopes - should refactor based on type
    // Note scopes and roles are processed as OR
    // TODO: Create logic to process policies that we use in the API
    public async Task Invoke(HttpContext context)
    {
        var route = context.Items.DownstreamRoute();
        var options = route.AuthenticationOptions;
        if (!context.IsOptionsMethod() && route.IsAuthenticated)
        {
            var authorized = _scopesAuthorizer.Authorize(context.User, options.AllowedScopes, options.ScopeKey);
            if (authorized.IsError)
            {
#if DEBUG
                Logger.LogWarning(() => $"The '{route.Name()}' route encountered authorization errors due to user scopes:{authorized.Errors.ToErrorString(true)}");
#endif
                context.Items.UpsertErrors(authorized.Errors);
                return;
            }

            if (!authorized.Data) // TODO: Looks like this is never called due to the current ScopesAuthorizer design :D Definitely a good reason to refactor
            {
                var error = new UnauthorizedError($"{context.User.Identity.Name} unable to access route {route.Name()}");
#if DEBUG
                Logger.LogInformation(error.ToString);
#endif
                context.Items.SetError(error);
            }
        }

        if (!context.IsOptionsMethod() && route.IsAuthenticated)
        {
            Logger.LogInformation("route and scope is authenticated role must be checked");
            var authorizedRole = _rolesAuthorizer.Authorize(context.User, options.RequiredRole, options.RoleKey);
            if (authorizedRole.IsError)
            {
                Logger.LogWarning("error authorizing user roles");
                context.Items.UpsertErrors(authorizedRole.Errors);
                return;
            }

            if (authorizedRole.Data)
            {
                Logger.LogInformation("user has the required role and is authorized calling next authorization checks");
            }
            else
            {
                Logger.LogWarning("user does not have the required role and is not authorized setting pipeline error");
                context.Items.SetError(new UnauthorizedError(
                    $"{context.User.Identity.Name} unable to access {route.UpstreamPathTemplate.OriginalValue}"));
            }
        }

        if (!context.IsOptionsMethod() && route.IsAuthorized)
        {
            var authorized = _claimsAuthorizer.Authorize(context.User, route.RouteClaimsRequirement, context.Items.TemplatePlaceholderNameAndValues());
            if (authorized.IsError)
            {
#if DEBUG
                Logger.LogWarning(() => $"Error whilst authorizing {context.User.Identity.Name} in route {route.Name()}:{authorized.Errors.ToErrorString(true)}");
#endif
                context.Items.UpsertErrors(authorized.Errors);
                return;
            }

            if (authorized.Data)
            {
#if DEBUG
                Logger.LogInformation(() => $"{context.User.Identity.Name} has successfully been authorized for {route.Name()}.");
#endif
                await _next.Invoke(context);
            }
            else
            {
                var error = new UnauthorizedError($"{context.User.Identity.Name} is not authorized to access '{route.Name()}' route. Setting pipeline error.");
#if DEBUG
                Logger.LogInformation(error.ToString);
#endif
                context.Items.SetError(error);
            }
        }
        else
        {
#if DEBUG
            Logger.LogDebug(() => $"No authorization needed for the route: {route.Name()}");
#endif
            await _next.Invoke(context);
        }
    }
}
