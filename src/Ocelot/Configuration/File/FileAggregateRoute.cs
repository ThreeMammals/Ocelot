namespace Ocelot.Configuration.File;

public class FileAggregateRoute : IRoute
{
    public string Aggregator { get; set; }
    public int Priority { get; set; }
    public bool RouteIsCaseSensitive { get; set; }
    public List<string> RouteKeys { get; set; }
    public List<AggregateRouteConfig> RouteKeysConfig { get; set; }
    public IDictionary<string, string> UpstreamHeaderTemplates { get; set; }
    public string UpstreamHost { get; set; }
    public List<string> UpstreamHttpMethod { get; set; }
    public string UpstreamPathTemplate { get; set; }

    public FileAggregateRoute()
    {
        Priority = 1;
        RouteKeys = new();
        RouteKeysConfig = new();
        UpstreamHeaderTemplates = new Dictionary<string, string>();
        UpstreamHttpMethod = new();
    }

    /// <summary>This allows to override global default HTTP verb value.</summary>
    /// <remarks>Defaults: The <see cref="HttpMethod.Get"/> value.</remarks>
    /// <value>A <see cref="HttpMethod"/> value.</value>
    public static HttpMethod DefaultHttpMethod { get; set; } = HttpMethod.Get;
}
