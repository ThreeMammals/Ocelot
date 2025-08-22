namespace Ocelot.Configuration;

public class RouteOptions
{
    public RouteOptions(bool isAuthenticated, bool isAuthorized, bool isCached, bool useServiceDiscovery)
    {
        IsAuthenticated = isAuthenticated;
        IsAuthorized = isAuthorized;
        IsCached = isCached;
        UseServiceDiscovery = useServiceDiscovery;
    }

    public bool IsAuthenticated { get; }
    public bool IsAuthorized { get; }
    public bool IsCached { get; }
    public bool UseServiceDiscovery { get; }
}
