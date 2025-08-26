namespace Ocelot.Configuration.File;

public sealed class FileGlobalRateLimit : FileRateLimitOptions // TODO This is temporarily solution to inherit from RL by Header feature model, an extraction of props is required
    , IRateLimitingGroupByKeys // Default grouping technique by keys (inspired by the Aggregation feature) used to apply global rules. TODO Apply the interface for Aggregation feat models
{
    //public bool DisableRateLimitHeaders { get; set; }
    //public IList<string> ClientWhitelist { get; set; }
    //public int HttpStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;
    //public string ClientIdHeader { get; set; } = "ClientId";
    //public string QuotaExceededMessage { get; set; }
    //public string RateLimitCounterPrefix { get; set; } = "ocelot";
    public bool EnableRateLimiting { get; init; } = true;

    // TODO Potentially, it should be 'Policy Name', or something that conveys the meaning of 'Rule Name'
    public string Name { get; init; }

    public string Pattern { get; init; }
    public int Limit { get; init; }
    public string Period { get; init; }
    public double PeriodTimespan { get; init; }
}
