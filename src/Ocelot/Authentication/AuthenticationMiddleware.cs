using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Authentication;

public sealed class AuthenticationMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next, IOcelotLoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<AuthenticationMiddleware>())
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var path = httpContext.Request.Path;
        var route = httpContext.Items.DownstreamRoute();

        if (request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) || !route.IsAuthenticated)
        {
            Logger.LogInformation(() => $"No authentication is required for the path '{path}' in the route {route.Name()}.");
            await _next(httpContext);
            return;
        }

        Logger.LogInformation(() => $"The path '{path}' is an authenticated route! {MiddlewareName} checking if client is authenticated...");

        var result = await AuthenticateAsync(httpContext, route);
        if (result.Principal?.Identity == null)
        {
            SetUnauthenticatedError(httpContext, path, null);
            return;
        }

        httpContext.User = result.Principal;
        if (httpContext.User.Identity.IsAuthenticated)
        {
            Logger.LogInformation(() => $"Client has been authenticated for path '{path}' by '{httpContext.User.Identity.AuthenticationType}' scheme.");
            await _next.Invoke(httpContext);
            return;
        }

        SetUnauthenticatedError(httpContext, path, httpContext.User.Identity.Name);
    }

    private void SetUnauthenticatedError(HttpContext httpContext, string path, string userName)
    {
        var error = new UnauthenticatedError($"Request for authenticated route '{path}' {(string.IsNullOrEmpty(userName) ? "was unauthenticated" : $"by '{userName}' was unauthenticated!")}");
        Logger.LogWarning(() => $"Client has NOT been authenticated for path '{path}' and pipeline error set. {error};");
        httpContext.Items.SetError(error);
    }

    private async Task<AuthenticateResult> AuthenticateAsync(HttpContext context, DownstreamRoute route)
    {
        var notEmptySchemes = route.AuthenticationOptions.AuthenticationProviderKeys
            .Where(s => !string.IsNullOrWhiteSpace(s));
        AuthenticateResult result = null;
        foreach (var scheme in notEmptySchemes)
        {
            try
            {
                result = await context.AuthenticateAsync(scheme);
                if (result?.Succeeded == true)
                {
                    return result;
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning(() =>
                    $"Unable to authenticate the client for route '{route.Name()}' using the {scheme} authentication scheme due to error: {e.Message}");
            }
        }

        return result ?? AuthenticateResult.NoResult();
    }
}
