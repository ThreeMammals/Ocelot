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

        // TODO: This sounds more like a hack, it might be better to refactor this at some point.
        var isCached = route.FileCacheOptions.TtlSeconds > 0;
        var useServiceDiscovery = !string.IsNullOrEmpty(route.ServiceName);

        return new RouteOptionsBuilder()
            .WithIsAuthenticated(isAuthenticated)
            .WithIsAuthorized(isAuthorized)
            .WithIsCached(isCached)
            .WithUseServiceDiscovery(useServiceDiscovery)
            .Build();
    }
}
