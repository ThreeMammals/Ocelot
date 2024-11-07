using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Authentication.Middleware
{
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
            var downstreamRoute = httpContext.Items.DownstreamRoute();

            // reducing nesting, returning early when no authentication is needed.
            if (request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) || !downstreamRoute.IsAuthenticated)
            {
                Logger.LogInformation($"No authentication needed for path '{path}'.");
                await _next(httpContext);
                return;
            }

            Logger.LogInformation(() => $"The path '{path}' is an authenticated route! {MiddlewareName} checking if client is authenticated...");

            var result = await AuthenticateAsync(httpContext, downstreamRoute);

            if (result.Principal?.Identity == null)
            {
                await ChallengeAsync(httpContext, downstreamRoute, result);
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

            await ChallengeAsync(httpContext, downstreamRoute, result);
            SetUnauthenticatedError(httpContext, path, httpContext.User.Identity.Name);
        }

        private void SetUnauthenticatedError(HttpContext httpContext, string path, string userName)
        {
            var error = new UnauthenticatedError($"Request for authenticated route '{path}' {(string.IsNullOrEmpty(userName) ? "was unauthenticated" : $"by '{userName}' was unauthenticated!")}");
            Logger.LogWarning(() => $"Client has NOT been authenticated for path '{path}' and pipeline error set. {error};");
            httpContext.Items.SetError(error);
        }

        private async Task ChallengeAsync(HttpContext context, DownstreamRoute route, AuthenticateResult status)
        {
            // Perform a challenge. This populates the WWW-Authenticate header on the response
            await context.ChallengeAsync(route.AuthenticationOptions.AuthenticationProviderKey); // TODO Read failed scheme from auth result

            // Since the response gets re-created down the pipeline, we store the challenge in the Items, so we can re-apply it when sending the response
            if (context.Response.Headers.TryGetValue("WWW-Authenticate", out var authenticateHeader))
            {
                context.Items.SetAuthChallenge(authenticateHeader);
            }
        }

        private async Task<AuthenticateResult> AuthenticateAsync(HttpContext context, DownstreamRoute route)
        {
            var options = route.AuthenticationOptions;
            if (!string.IsNullOrWhiteSpace(options.AuthenticationProviderKey))
            {
                return await context.AuthenticateAsync(options.AuthenticationProviderKey);
            }

            var providerKeys = options.AuthenticationProviderKeys;
            if (providerKeys.Length == 0 || providerKeys.All(string.IsNullOrWhiteSpace))
            {
                Logger.LogWarning(() => $"Impossible to authenticate client for path '{route.DownstreamPathTemplate}': both {nameof(options.AuthenticationProviderKey)} and {nameof(options.AuthenticationProviderKeys)} are empty but the {nameof(Configuration.AuthenticationOptions)} have defined.");
                return AuthenticateResult.NoResult();
            }

            AuthenticateResult result = null;
            foreach (var scheme in providerKeys.Where(apk => !string.IsNullOrWhiteSpace(apk)))
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
                        $"Impossible to authenticate client for path '{route.DownstreamPathTemplate}' and {nameof(options.AuthenticationProviderKey)}:{scheme}. Error: {e.Message}.");
                }
            }

            return result ?? AuthenticateResult.NoResult();
        }
    }
}
