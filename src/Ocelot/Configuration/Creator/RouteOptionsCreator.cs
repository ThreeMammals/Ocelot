using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class RouteOptionsCreator : IRouteOptionsCreator
{
    public RouteOptions Create(FileRoute route, FileGlobalConfiguration global)
    {
        if (route == null)
        {
            return new RouteOptionsBuilder().Build();
        }

        // TODO Requires design review, see code review
        bool isAuthenticated = route?.AuthenticationOptions?.AllowAnonymous != true && global?.AuthenticationOptions?.HasScheme == true
            || route?.AuthenticationOptions?.HasScheme == true;

        bool isAuthorized = (route.RouteClaimsRequirement?.Count ?? 0) > 0;

        // TODO: This sounds more like a hack, it might be better to refactor this at some point.
        var isCached = route.FileCacheOptions.TtlSeconds > 0;
        var enableRateLimiting = route.RateLimitOptions?.EnableRateLimiting == true;
        var useServiceDiscovery = !string.IsNullOrEmpty(route.ServiceName);

        return new RouteOptionsBuilder()
            .WithIsAuthenticated(isAuthenticated)
            .WithIsAuthorized(isAuthorized)
            .WithIsCached(isCached)
            .WithRateLimiting(enableRateLimiting)
            .WithUseServiceDiscovery(useServiceDiscovery)
            .Build();
    }
}
