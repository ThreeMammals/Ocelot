namespace Ocelot.Configuration.File;

public class FileRateLimitRule
{
    public FileRateLimitRule() { }

    public FileRateLimitRule(FileRateLimitRule from)
    {
        ArgumentNullException.ThrowIfNull(from);

        EnableRateLimiting = from.EnableRateLimiting;
        EnableHeaders = from.EnableHeaders;
        Limit = from.Limit;
        Period = from.Period;
        PeriodTimespan = from.PeriodTimespan;
    }

    /// <summary>Enables or disables rate limiting. If undefined, it implicitly defaults to <see langword="true"/> (enabled).</summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see langword="bool"/>.</value>
    public bool? EnableRateLimiting { get; set; }

    /// <summary>Enables or disables <c>X-Rate-Limit-*</c> and <c>Retry-After</c> headers.</summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see langword="bool"/>.</value>
    public bool? EnableHeaders { get; set; }

    /// <summary>The maximum number of requests a client can make within a given time <see cref="Period"/>.</summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see cref="long"/>.</value>
    public long? Limit { get; set; }

    /// <summary>Rate limit period can be expressed as 1 second (1s), 1 minute (1m), 1 hour (1h), or 1 day (1d).</summary>
    /// <value>A <see cref="string"/> object.</value>
    public string Period { get; set; }

    /// <summary>The time interval to wait before sending a new request, measured in seconds.</summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see cref="double"/>.</value>
    public double? PeriodTimespan { get; set; } // TODO It could be a string to be parsed

    /// <inheritdoc/>
    public override string ToString() => EnableRateLimiting == false ? string.Empty
        : $"{nameof(Limit)}:{Limit},{nameof(Period)}:{Period},{nameof(PeriodTimespan)}:{PeriodTimespan:F}";
}
