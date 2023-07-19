namespace Ocelot.Configuration
{
    public class RouteOptions
    {
        public RouteOptions(bool isAuthenticated, bool isAuthorized, bool isCached, bool isEnableRateLimiting, bool useServiceDiscovery)
        {
            IsAuthenticated = isAuthenticated;
            IsAuthorized = isAuthorized;
            IsCached = isCached;
            EnableRateLimiting = isEnableRateLimiting;
            UseServiceDiscovery = useServiceDiscovery;
        }

        public bool IsAuthenticated { get; }
        public bool IsAuthorized { get; }
        public bool IsCached { get; }
        public bool EnableRateLimiting { get; }
        public bool UseServiceDiscovery { get; }
    }
}
