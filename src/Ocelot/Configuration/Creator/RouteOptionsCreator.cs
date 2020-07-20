using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class RouteOptionsCreator : IRouteOptionsCreator
    {
        public RouteOptions Create(FileRoute fileRoute, FileGlobalConfiguration globalConfiguration)
        {
            var isAuthenticated = IsAuthenticated(fileRoute, globalConfiguration);
            var isAuthorized = IsAuthorized(fileRoute);
            var isCached = IsCached(fileRoute);
            var enableRateLimiting = IsEnableRateLimiting(fileRoute);
            var useServiceDiscovery = !string.IsNullOrEmpty(fileRoute.ServiceName);

            var options = new RouteOptionsBuilder()
                .WithIsAuthenticated(isAuthenticated)
                .WithIsAuthorized(isAuthorized)
                .WithIsCached(isCached)
                .WithRateLimiting(enableRateLimiting)
                .WithUseServiceDiscovery(useServiceDiscovery)
                .Build();

            return options;
        }

        private static bool IsEnableRateLimiting(FileRoute fileRoute) => fileRoute.RateLimitOptions?.EnableRateLimiting == true;

        private static bool IsAuthenticated(FileRoute fileRoute, FileGlobalConfiguration globalConfiguration)
        {
            return (!string.IsNullOrEmpty(globalConfiguration?.AuthenticationOptions?.AuthenticationProviderKey) &&
                                                              !fileRoute.AuthenticationOptions.AllowAnonymousForGlobalAuthenticationOptions) ||
                !string.IsNullOrEmpty(fileRoute.AuthenticationOptions?.AuthenticationProviderKey);
        }

        private static bool IsAuthorized(FileRoute fileRoute) => fileRoute.RouteClaimsRequirement?.Count > 0;

        private static bool IsCached(FileRoute fileRoute) => fileRoute.FileCacheOptions.TtlSeconds > 0;
    }
}
