using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class ReRouteOptionsCreator : IReRouteOptionsCreator
    {
        public ReRouteOptions Create(FileReRoute fileReRoute)
        {
            var isAuthenticated = IsAuthenticated(fileReRoute);
            var isAuthorised = IsAuthorised(fileReRoute);
            var isCached = IsCached(fileReRoute);
            var enableRateLimiting = IsEnableRateLimiting(fileReRoute);
            var useServiceDiscovery = !string.IsNullOrEmpty(fileReRoute.ServiceName);

            var options = new ReRouteOptionsBuilder()
                .WithIsAuthenticated(isAuthenticated)
                .WithIsAuthorised(isAuthorised)
                .WithIsCached(isCached)
                .WithRateLimiting(enableRateLimiting)
                .WithUseServiceDiscovery(useServiceDiscovery)
                .Build();

            return options;
        }

        private static bool IsEnableRateLimiting(FileReRoute fileReRoute)
        {
            return (fileReRoute.RateLimitOptions != null && fileReRoute.RateLimitOptions.EnableRateLimiting) ? true : false;
        }

        private bool IsAuthenticated(FileReRoute fileReRoute)
        {
            return !string.IsNullOrEmpty(fileReRoute.AuthenticationOptions?.AuthenticationProviderKey);
        }

        private bool IsAuthorised(FileReRoute fileReRoute)
        {
            return fileReRoute.RouteClaimsRequirement?.Count > 0;
        }

        private bool IsCached(FileReRoute fileReRoute)
        {
            return fileReRoute.FileCacheOptions.TtlSeconds > 0;
        }
    }
}
