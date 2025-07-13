namespace Ocelot.Configuration.File;

public interface IRoute
{
    IDictionary<string, string> UpstreamHeaderTemplates { get; set; }
    string UpstreamPathTemplate { get; set; }
    bool RouteIsCaseSensitive { get; set; }
    int Priority { get; set; }
}
