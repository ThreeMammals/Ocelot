using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Security.Claims;

namespace Ocelot.Authentication.Middleware
{
    public sealed class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;

        public AuthenticationMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IMemoryCache memoryCache)
            : base(loggerFactory.CreateLogger<AuthenticationMiddleware>())
        {
            _next = next;
            _memoryCache = memoryCache;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var path = httpContext.Request.Path;
            var downstreamRoute = httpContext.Items.DownstreamRoute();

            // reducing nesting, returning early when no authentication is needed.
            if (request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) || !downstreamRoute.IsAuthenticated)
            {
                Logger.LogInformation(() => $"No authentication needed for path '{path}'.");
                await _next(httpContext);
                return;
            }

            Logger.LogInformation(() => $"The path '{path}' is an authenticated route! {MiddlewareName} checking if client is authenticated...");
            var token = httpContext.Request.Headers.FirstOrDefault(r => r.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
            var cacheKey = "identityToken." + token;
            if (!_memoryCache.TryGetValue(cacheKey, out ClaimsPrincipal principal))
            {
                var auth = await AuthenticateAsync(httpContext, downstreamRoute);
                principal = auth.Principal;
                _memoryCache.Set(cacheKey, principal, TimeSpan.FromMinutes(5));
            }

            if (principal?.Identity == null)
            {
                SetUnauthenticatedError(httpContext, path, null);
                return;
            }

            httpContext.User = principal;
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
