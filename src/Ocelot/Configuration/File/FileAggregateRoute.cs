using Microsoft.AspNetCore.Http;

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
    public IList<string> UpstreamHttpMethod { get; set; }
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
        UpstreamHttpMethod = [ HttpMethods.Get ]; // Only supports GET..are you crazy!! POST, PUT WOULD BE CRAZY!! :)
        UpstreamPathTemplate = default;
    }
}
