using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.Configuration;

public class Route
{
    public Route() => DownstreamRoute = new();
    public Route(bool isDynamic) : this() => IsDynamic = isDynamic;
    public Route(bool isDynamic, DownstreamRoute route) : this(route) => IsDynamic = isDynamic;
    public Route(DownstreamRoute route) => DownstreamRoute = [route];
    public Route(DownstreamRoute route, HttpMethod method)
    {
        DownstreamRoute = [route];
        UpstreamHttpMethod = [method];
    }

    public bool IsDynamic { get; }
    public string Aggregator { get; init; }
    public List<DownstreamRoute> DownstreamRoute { get; init; }
    public List<AggregateRouteConfig> DownstreamRouteConfig { get; init; }
    public IDictionary<string, UpstreamHeaderTemplate> UpstreamHeaderTemplates { get; init; }
    public string UpstreamHost { get; init; }
    public HashSet<HttpMethod> UpstreamHttpMethod { get; init; }
    public UpstreamPathTemplate UpstreamTemplatePattern { get; init; }
}
