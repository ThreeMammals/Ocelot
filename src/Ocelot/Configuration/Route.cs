using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.Configuration;

public class Route
{
    public Route() => DownstreamRoute = new();
    public Route(DownstreamRoute route) => DownstreamRoute = [route];
    public Route(DownstreamRoute route, HttpMethod method)
    {
        DownstreamRoute = [route];
        UpstreamHttpMethod = [method];
    }

    public string Aggregator { get; init; }
    public List<DownstreamRoute> DownstreamRoute { get; init; }
    public List<AggregateRouteConfig> DownstreamRouteConfig { get; init; }
    public IDictionary<string, UpstreamHeaderTemplate> UpstreamHeaderTemplates { get; init; }
    public string UpstreamHost { get; init; }
    public HashSet<HttpMethod> UpstreamHttpMethod { get; init; }
    public UpstreamPathTemplate UpstreamTemplatePattern { get; init; }
}
