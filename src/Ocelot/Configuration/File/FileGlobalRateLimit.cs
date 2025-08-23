using Microsoft.AspNetCore.Http;

namespace Ocelot.Configuration.File;

public sealed record FileGlobalRateLimit
{
    public string Name { get; init; }
    public string Pattern { get; init; } 
    public IList<string> Methods { get; init; }
    public int Limit { get; init; }
    public string Period { get; init; }
    public double PeriodTimespan { get; init; }
    public int HttpStatusCode { get; init; } = StatusCodes.Status429TooManyRequests;
    public string QuotaExceededMessage { get; init; }
    public bool DisableRateLimitHeaders { get; init; }
    public bool EnableRateLimiting { get; init; }
    public IList<string> ClientWhitelist { get; set; }
}
