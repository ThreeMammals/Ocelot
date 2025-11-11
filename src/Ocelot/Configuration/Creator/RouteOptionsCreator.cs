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
            return new();
        }

        //route.AuthenticationOptions ??= new();
        //global.AuthenticationOptions ??= new();
        //bool isAuthenticated = route.AuthenticationOptions.AllowAnonymous != true
        //    && (route.AuthenticationOptions.HasScheme || global.AuthenticationOptions.HasScheme);
        bool isAuthorized = (route.RouteClaimsRequirement?.Count ?? 0) > 0;

        var useServiceDiscovery = !string.IsNullOrEmpty(route.ServiceName);

        return new(useServiceDiscovery);
            //.WithIsAuthenticated(isAuthenticated)
            //.WithIsAuthorized(isAuthorized)
            //.WithUseServiceDiscovery(useServiceDiscovery)
            //.Build();
    }
}
