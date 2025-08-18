namespace Ocelot.Configuration.File;

public interface IRouteUpstream
{
    IDictionary<string, string> UpstreamHeaderTemplates { get; }
    string UpstreamPathTemplate { get; }
    List<string> UpstreamHttpMethod { get; }
    bool RouteIsCaseSensitive { get; }
    int Priority { get; }
}
