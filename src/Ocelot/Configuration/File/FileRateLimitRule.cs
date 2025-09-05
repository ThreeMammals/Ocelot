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
        StatusCode = from.StatusCode;
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

    /// <summary>Gets or sets the rejection status code returned during the Quota Exceeded period, aka the <see cref="Wait"/> window, or the remainder of the <see cref="Period"/> fixed window following the moment of exceeding.
    /// <para>Default value: <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/429">429 (Too Many Requests)</see>.</para></summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see cref="int"/>.</value>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Returns a string that represents the current rule in the format, which defaults to empty string if rate limiting is disabled (<see cref="EnableRateLimiting"/> is <see langword="false"/>).
    /// </summary>
    /// <remarks>Format: <c>H{+,-}:{limit}:{period,-}:w{wait,-}</c>.</remarks>
    /// <returns>A <see cref="string"/> object.</returns>
    public override string ToString() => EnableRateLimiting == false ? string.Empty
        : $"H{(EnableHeaders == false ? '-' : '+')}:{Limit}:{Period.IfEmpty(None)}:w{(PeriodTimespan.HasValue ? PeriodTimespan.Value.ToString("F") : Wait.IfEmpty(None))}";

    public const string None = "-";
}
