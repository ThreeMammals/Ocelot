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
            var isQos = IsQoS(fileReRoute);
            var enableRateLimiting = IsEnableRateLimiting(fileReRoute);

            var options = new ReRouteOptionsBuilder()
                .WithIsAuthenticated(isAuthenticated)
                .WithIsAuthorised(isAuthorised)
                .WithIsCached(isCached)
                .WithIsQos(isQos)
                .WithRateLimiting(enableRateLimiting)
                .Build();
            
            return options;
        }

        private static bool IsEnableRateLimiting(FileReRoute fileReRoute)
        {
            return (fileReRoute.RateLimitOptions != null && fileReRoute.RateLimitOptions.EnableRateLimiting) ? true : false;
        }

        private bool IsQoS(FileReRoute fileReRoute)
        {
            return fileReRoute.QoSOptions?.ExceptionsAllowedBeforeBreaking > 0 && fileReRoute.QoSOptions?.TimeoutValue > 0;
        }

        private bool IsAuthenticated(FileReRoute fileReRoute)
        {
            return !string.IsNullOrEmpty(fileReRoute.AuthenticationOptions?.Provider);
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