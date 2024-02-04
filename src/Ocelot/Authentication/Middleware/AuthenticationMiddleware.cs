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

        public AuthenticationMiddleware(
            RequestDelegate next,
            IOcelotLoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<AuthenticationMiddleware>())
        {
            _next = next;
        }

        protected override string MiddlewareName => nameof(AuthenticationMiddleware);

        public async Task Invoke(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var path = httpContext.Request.Path;
            var downstreamRoute = httpContext.Items.DownstreamRoute();

            if (!request.Method.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase) && downstreamRoute.IsAuthenticated)
            {
                Logger.LogInformation(() => $"The path '{path}' is an authenticated route! {MiddlewareName} checking if client is authenticated...");

                var result = await AuthenticateAsync(httpContext, downstreamRoute);
                httpContext.User = result.Principal;
                var identity = httpContext.User.Identity;

                if (identity.IsAuthenticated)
                {
                    Logger.LogInformation(() => $"Client has been authenticated for path '{path}' by '{identity.AuthenticationType}' scheme.");
                    await _next.Invoke(httpContext);
                }
                else
                {
                    var error = new UnauthenticatedError($"Request for authenticated route '{path}' by '{identity.Name}' was unauthenticated!");
                    Logger.LogWarning(() => $"Client has NOT been authenticated for path '{path}' and pipeline error set. {error};");
                    httpContext.Items.SetError(error);
                }
            }
            else
            {
                Logger.LogInformation(() => $"No authentication needed for path '{path}'.");
                await _next.Invoke(httpContext);
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
                    Logger.LogWarning(() => $"Impossible to authenticate client for path '{route.DownstreamPathTemplate}' and {nameof(options.AuthenticationProviderKey)}:{scheme}. Error: {e.Message}.");
                }
            }

            return result ?? AuthenticateResult.NoResult();
        }
    }
}
