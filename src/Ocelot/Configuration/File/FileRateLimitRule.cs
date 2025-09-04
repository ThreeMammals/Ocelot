using Ocelot.Infrastructure.Extensions;

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
        Wait = from.Wait;
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

    /// <summary>Rate limiting period (fixed window) can be expressed as milliseconds (1ms), as seconds (1s), minutes (1m), hours (1h), or days (1d).</summary>
    /// <remarks>Defaults: If no unit is specified, the default unit is 'ms'.</remarks>
    /// <value>A <see cref="string"/> object.</value>
    public string Period { get; set; }

    /// <summary>The time interval to wait before sending a new request, measured in seconds.</summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see cref="double"/>.</value>
    [Obsolete("Use Wait instead of PeriodTimespan! Note that PeriodTimespan will be removed in version 25.0.")]
    public double? PeriodTimespan { get; set; }

    /// <summary>Rate limiting wait window (no servicing window) can be expressed as milliseconds (1ms), as seconds (1s), minutes (1m), hours (1h), or days (1d).</summary>
    /// <remarks>Defaults: If no unit is specified, the default unit is 'ms'.</remarks>
    /// <value>A <see cref="string"/> object.</value>
    public string Wait { get; set; }

    /// <inheritdoc/>
    public override string ToString() => EnableRateLimiting == false ? string.Empty
        : $"H{(EnableHeaders == true ? '+' : '-')}:{Limit}:{Period}:w{(PeriodTimespan.HasValue ? PeriodTimespan.Value.ToString("F") : Wait.IfEmpty("-"))}";
}
