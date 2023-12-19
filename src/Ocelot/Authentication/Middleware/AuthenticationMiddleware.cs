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
            var downstreamRoute = httpContext.Items.DownstreamRoute();

            if (httpContext.Request.Method.ToUpper() != "OPTIONS" && downstreamRoute.IsAuthenticated)
            {
                Logger.LogInformation(() => $"{httpContext.Request.Path} is an authenticated route. {MiddlewareName} checking if client is authenticated");

                var result = await AuthenticateAsync(httpContext, downstreamRoute);

                httpContext.User = result.Principal;

                if (httpContext.User.Identity.IsAuthenticated)
                {
                    Logger.LogInformation(() => $"Client has been authenticated for {httpContext.Request.Path}");
                    await _next.Invoke(httpContext);
                }
                else
                {
                    var error = new UnauthenticatedError(
                        $"Request for authenticated route {httpContext.Request.Path} by {httpContext.User.Identity.Name} was unauthenticated");

                    Logger.LogWarning(() => $"Client has NOT been authenticated for {httpContext.Request.Path} and pipeline error set. {error}");

                    httpContext.Items.SetError(error);
                }
            }
            else
            {
                Logger.LogInformation(() => $"No authentication needed for {httpContext.Request.Path}");

                await _next.Invoke(httpContext);
            }
        }

        private static async Task<AuthenticateResult> AuthenticateAsync(HttpContext httpContext, DownstreamRoute route)
        {
            var options = route.AuthenticationOptions;
            if (!string.IsNullOrWhiteSpace(options.AuthenticationProviderKey))
            {
                return await httpContext.AuthenticateAsync(options.AuthenticationProviderKey);
            }

            if (options.AuthenticationProviderKeys.Length == 0)
            {
                return AuthenticateResult.NoResult();
            }

            var keys = options.AuthenticationProviderKeys
                .Where(apk => !string.IsNullOrWhiteSpace(apk));

            foreach (var authenticationProviderKey in keys)
            {
                var result = await httpContext.AuthenticateAsync(authenticationProviderKey);
                if (result.Succeeded)
                {
                    return result;
                }
            }

            return AuthenticateResult.NoResult();
        }
    }
}
