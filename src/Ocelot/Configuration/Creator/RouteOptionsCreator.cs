using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class RouteOptionsCreator : IRouteOptionsCreator
    {
        public RouteOptions Create(FileRoute fileRoute)
        {
            if (fileRoute == null)
            {
                return new RouteOptionsBuilder().Build();
            }

            var authOpts = fileRoute.AuthenticationOptions;
            var isAuthenticated = authOpts != null
                && (!string.IsNullOrEmpty(authOpts.AuthenticationProviderKey)
                    || authOpts.AuthenticationProviderKeys?.Any(k => !string.IsNullOrWhiteSpace(k)) == true);
            var isAuthorized = fileRoute.RouteClaimsRequirement?.Any() == true;
            var isCached = fileRoute.FileCacheOptions.TtlSeconds > 0;
            var enableRateLimiting = fileRoute.RateLimitOptions?.EnableRateLimiting == true;
            var useServiceDiscovery = !string.IsNullOrEmpty(fileRoute.ServiceName);

            return new RouteOptionsBuilder()
                .WithIsAuthenticated(isAuthenticated)
                .WithIsAuthorized(isAuthorized)
                .WithIsCached(isCached)
                .WithRateLimiting(enableRateLimiting)
                .WithUseServiceDiscovery(useServiceDiscovery)
                .Build();
        }
    }
}
