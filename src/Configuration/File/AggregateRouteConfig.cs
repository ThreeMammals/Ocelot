namespace Ocelot.Configuration.File;

public class AggregateRouteConfig(string routeKey, string jsonPath, string parameter)
{
    public string RouteKey { get; set; } = routeKey;
    public string Parameter { get; set; } = parameter;
    public string JsonPath { get; set; } = jsonPath;
}
