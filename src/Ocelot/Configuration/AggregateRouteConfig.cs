namespace Ocelot.Configuration
{
    public class AggregateRouteConfig
    {
        public AggregateRouteConfig(string routeId, string parameter, string jsonPath)
        {
            RouteId = routeId;
            Parameter = parameter;
            JsonPath = jsonPath;
        }

        public string RouteId { get; private set; }
        public string Parameter { get; private set; }
        public string JsonPath { get; private set; }
    }
}
