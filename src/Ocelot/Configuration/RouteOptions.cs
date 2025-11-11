namespace Ocelot.Configuration;

public class RouteOptions
{
    public RouteOptions() { }
    public RouteOptions(/*bool isAuthenticated, bool isAuthorized,*/ bool useServiceDiscovery)
    {
        //IsAuthenticated = isAuthenticated;
        //IsAuthorized = isAuthorized;
        UseServiceDiscovery = useServiceDiscovery;
    }

    //public bool IsAuthenticated { get; }
    //public bool IsAuthorized { get; }
    public bool UseServiceDiscovery { get; }
}
