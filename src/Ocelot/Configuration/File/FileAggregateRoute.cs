namespace Ocelot.Configuration.File;

public class FileAggregateRoute : IRouteUpstream, IRouteGroup
{
    public string Aggregator { get; set; }
    public int Priority { get; set; } = 1;
    public bool RouteIsCaseSensitive { get; set; }
    public HashSet<string> RouteKeys { get; set; }
    public List<AggregateRouteConfig> RouteKeysConfig { get; set; }
    public IDictionary<string, string> UpstreamHeaderTemplates { get; set; }
    public string UpstreamHost { get; set; }
    public HashSet<string> UpstreamHttpMethod { get; set; }
    public string UpstreamPathTemplate { get; set; }

    public FileAggregateRoute()
    {
        Aggregator = default;
        Priority = 1;
        RouteIsCaseSensitive = default;
        RouteKeys = new();
        RouteKeysConfig = new();
        UpstreamHeaderTemplates = new Dictionary<string, string>();
        UpstreamHost = default;
        UpstreamHttpMethod = [];
        UpstreamPathTemplate = default;
    }

    /// <summary>This allows to override global default HTTP verb value.</summary>
    /// <remarks>Defaults: The <see cref="HttpMethod.Get"/> value.</remarks>
    /// <value>A <see cref="HttpMethod"/> value.</value>
    public static HttpMethod DefaultHttpMethod { get; set; } = HttpMethod.Get;
}
