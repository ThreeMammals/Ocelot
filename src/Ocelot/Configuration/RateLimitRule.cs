namespace Ocelot.Configuration;

public class RateLimitRule
{
    public const string DefaultPeriod = "1s";
    public const double ZeroPeriodTimespan = 0.0D;
    public const long ZeroLimit = 0L;
    public static RateLimitRule Empty = new(DefaultPeriod, ZeroPeriodTimespan, ZeroLimit);

    public RateLimitRule(string period, double periodTimespan, long limit)
    {
        Period = string.IsNullOrWhiteSpace(period) ? DefaultPeriod : period;
        PeriodTimespan = Math.Abs(periodTimespan);
        Limit = Math.Abs(limit);
    }

    public override string ToString() => $"{Limit}/{Period}/w{PeriodTimespan:F}s";

    /// <summary>
    /// Rate limit period as in 1s, 1m, 1h, 1d.
    /// </summary>
    /// <value>
    /// A string value with rate limit period.
    /// </value>
    public string Period { get; }

    /// <summary>
    /// Timespan to wait after reaching the rate limit, in seconds.
    /// </summary>
    /// <value>
    /// A double floating-point integer with timespan, in seconds.
    /// </value>
    public double PeriodTimespan { get; }

    /// <summary>
    /// Maximum number of requests that a client can make in a defined period.
    /// </summary>
    /// <value>
    /// A long integer with maximum number of requests.
    /// </value>
    public long Limit { get; }
}
