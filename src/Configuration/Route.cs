using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;

namespace Ocelot.Configuration;

public class Route
{
    public Route() { }
    public Route(bool isDynamic) : this() => IsDynamic = isDynamic;
    public Route(bool isDynamic, DownstreamRoute route) : this(route) => IsDynamic = isDynamic;
    public Route(DownstreamRoute route) => DownstreamRoute.Add(route);
    public Route(DownstreamRoute route, HttpMethod method = null, IEnumerable<PlaceholderNameAndValue> placeholders = null)
    {
        DownstreamRoute.Add(route);
        UpstreamHttpMethod.Add(method ?? HttpMethod.Get);
        TemplatePlaceholderNameAndValues.AddRange(placeholders ?? []);
    }

    public bool IsDynamic { get; }
    public string Aggregator { get; init; }
    public List<DownstreamRoute> DownstreamRoute { get; init; } = [];
    public List<AggregateRouteConfig> DownstreamRouteConfig { get; init; }
    public List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues { get; init; } = [];
    public IDictionary<string, UpstreamHeaderTemplate> UpstreamHeaderTemplates { get; init; }
    public string UpstreamHost { get; init; }
    public HashSet<HttpMethod> UpstreamHttpMethod { get; init; } = [];
    public UpstreamPathTemplate UpstreamTemplatePattern { get; init; }
}
