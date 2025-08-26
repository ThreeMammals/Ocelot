namespace Ocelot.Configuration.File;

public class FileRateLimitRule
{
    public FileRateLimitRule() { }

    public FileRateLimitRule(FileRateLimitRule from)
    {
        ArgumentNullException.ThrowIfNull(from);

        EnableRateLimiting = from.EnableRateLimiting;
        Limit = from.Limit;
        Period = from.Period;
        PeriodTimespan = from.PeriodTimespan;
    }

    /// <summary>Enables or disables route rate limiting, which defaults to <see langword="true"/> (enabled).</summary>
    /// <value>A <see cref="bool"/> value.</value>
    public bool EnableRateLimiting { get; set; }

    /// <summary>The maximum number of requests a client can make within a given time <see cref="Period"/>.</summary>
    /// <value>A <see cref="long"/> value.</value>
    public long Limit { get; set; }

    /// <summary>Rate limit period can be expressed as 1 second (1s), 1 minute (1m), 1 hour (1h), or 1 day (1d).</summary>
    /// <value>A <see cref="string"/> object.</value>
    public string Period { get; set; }

    /// <summary>The time interval to wait before sending a new request, measured in seconds.</summary>
    /// <value>A <see cref="double"/> value.</value>
    public double PeriodTimespan { get; set; } // TODO It could be a string to be parsed

    /// <inheritdoc/>
    public override string ToString() => !EnableRateLimiting ? string.Empty
        : $"{nameof(Limit)}:{Limit},{nameof(Period)}:{Period},{nameof(PeriodTimespan)}:{PeriodTimespan:F}";
}
