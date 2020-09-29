namespace Ocelot.Configuration
{
    public class AggregateRouteConfig
    {
        public AggregateRouteConfig(RouteId routeId, string parameter, string jsonPath)
        {
            RouteId = routeId;
            Parameter = parameter;
            JsonPath = jsonPath;
        }

        public RouteId RouteId { get; private set; }
        public string Parameter { get; private set; }
        public string JsonPath { get; private set; }
    }
}
