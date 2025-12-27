using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;

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

        if (!context.IsOptionsMethod() && route.IsAuthenticated)
        {
            var authorized = _scopesAuthorizer.Authorize(context.User, route.AuthenticationOptions.AllowedScopes);
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
