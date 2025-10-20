using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class RouteOptionsCreator : IRouteOptionsCreator
{
    public RouteOptions Create(FileRoute route, FileGlobalConfiguration global)
    {
        global ??= new();
        if (route == null)
        {
            return new RouteOptionsBuilder().Build();
        }

        route.AuthenticationOptions ??= new();
        global.AuthenticationOptions ??= new();
        bool isAuthenticated = route.AuthenticationOptions.AllowAnonymous != true
            && (route.AuthenticationOptions.HasScheme || global.AuthenticationOptions.HasScheme);

        bool isAuthorized = (route.RouteClaimsRequirement?.Count ?? 0) > 0;

        int? ttlSeconds = route.FileCacheOptions?.TtlSeconds; // FileCacheOptions have the priority over CacheOptions
        ttlSeconds ??= route.CacheOptions?.TtlSeconds;
        ttlSeconds ??= global.CacheOptions?.TtlSeconds;
        var isCached = ttlSeconds.HasValue && ttlSeconds.Value > 0;

        var useServiceDiscovery = !string.IsNullOrEmpty(route.ServiceName);

        return new RouteOptionsBuilder()
            .WithIsAuthenticated(isAuthenticated)
            .WithIsAuthorized(isAuthorized)
            .WithIsCached(isCached)
            .WithUseServiceDiscovery(useServiceDiscovery)
            .Build();
    }
}
