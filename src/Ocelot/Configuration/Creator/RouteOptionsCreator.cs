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

        var authOpts = route.AuthenticationOptions;

        //var isAuthenticated = authOpts != null
        //    && (!string.IsNullOrEmpty(authOpts.AuthenticationProviderKey)
        //        || authOpts.AuthenticationProviderKeys?.Any(k => !string.IsNullOrWhiteSpace(k)) == true);
        var isAuthenticated = authOpts?.AllowAnonymous != true && global?.AuthenticationOptions?.HasProviderKey == true
            || authOpts?.HasProviderKey == true;
        var isAuthorized = route.RouteClaimsRequirement?.Any() == true;

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
