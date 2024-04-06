namespace Ocelot.Configuration.File;

public interface IRoute
{
    Dictionary<string, string> UpstreamHeaderTemplates { get; set; }
    string UpstreamPathTemplate { get; set; }
    bool RouteIsCaseSensitive { get; set; }
    int Priority { get; set; }
}
