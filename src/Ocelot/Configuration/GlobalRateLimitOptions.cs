namespace Ocelot.Configuration;

public class GlobalRateLimitOptions
{
    public string Name { get; set; }
    public Regex Pattern { get; init; } // wildcard -> regex
    public IReadOnlyList<string> Methods { get; init; } // TODO Change to HashSet<T> after Newtonsoft.Json removal
    public int Limit { get; init; }
    public string Period { get; init; }
    public int HttpStatusCode { get; init; }
    public string QuotaExceededMessage { get; init; }
    public bool DisableRateLimitHeaders { get; init; }
    public bool EnableRateLimiting { get; init; }
}
