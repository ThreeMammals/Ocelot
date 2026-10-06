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

    /// <summary>Initializes a new instance of the <see cref="Route"/> class.</summary>
    /// <remarks>This is the copying constructor which replaces the placeholders, keeping the original route unchanged.</remarks>
    /// <param name="from">The object to copy the properties from.</param>
    /// <param name="placeholders">The placeholders of the current request.</param>
    public Route(Route from, List<PlaceholderNameAndValue> placeholders)
    {
        IsDynamic = from.IsDynamic;
        Aggregator = from.Aggregator;
        DownstreamRoute = from.DownstreamRoute;
        DownstreamRouteConfig = from.DownstreamRouteConfig;
        TemplatePlaceholderNameAndValues = placeholders;
        UpstreamHeaderTemplates = from.UpstreamHeaderTemplates;
        UpstreamHost = from.UpstreamHost;
        UpstreamHttpMethod = from.UpstreamHttpMethod;
        UpstreamTemplatePattern = from.UpstreamTemplatePattern;
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
