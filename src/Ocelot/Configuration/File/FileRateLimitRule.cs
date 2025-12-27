using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.Infrastructure.Extensions;
using Ocelot.RateLimiting;

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
        QuotaMessage = from.QuotaMessage;
        KeyPrefix = from.KeyPrefix;
    }

    /// <summary>Enables or disables rate limiting. If undefined, it implicitly defaults to <see langword="true"/> (enabled).</summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see langword="bool"/>.</value>
    public bool? EnableRateLimiting { get; set; }

    /// <summary>Enables or disables <c>X-RateLimit-*</c> and <c>Retry-After</c> headers.</summary>
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
    [Obsolete("Use Wait instead of PeriodTimespan! Note that PeriodTimespan will be removed in version 25.0!")]
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
    /// Gets or sets a value to be used as the formatter for the Quota Exceeded response message.
    /// <para>If none specified the default will be: <see cref="RateLimitOptions.DefaultQuotaMessage"/>.</para>
    /// </summary>
    /// <value>A <see cref="string"/> value that will be used as a formatter.</value>
    public string QuotaMessage { get; set; }

    /// <summary>Gets or sets the counter prefix, used to compose the rate limiting counter caching key to be used by the <see cref="IRateLimitStorage"/> service.</summary>
    /// <remarks>Notes:
    /// <list type="number">
    /// <item>The consumer is the <see cref="IRateLimiting.GetStorageKey(ClientRequestIdentity, RateLimitOptions)"/> method.</item>
    /// <item>The property is relevant for distributed storage systems, such as <see cref="IDistributedCache"/> services, to inform users about which objects are being cached for management purposes.
    /// By default, each Ocelot instance uses its own <see cref="IMemoryCache"/> service without cross-instance synchronization.</item>
    /// </list>
    /// </remarks>
    /// <value>A <see cref="string"/> object which value defaults to "Ocelot.RateLimiting", see the <see cref="RateLimitOptions.DefaultCounterPrefix"/> property.</value>
    public string KeyPrefix { get; set; }

    /// <summary>
    /// Returns a string that represents the current rule in the format, which defaults to empty string if rate limiting is disabled (<see cref="EnableRateLimiting"/> is <see langword="false"/>).
    /// </summary>
    /// <remarks>Format: <c>H{+,-}:{limit}:{period,-}:w{wait,-}</c>.</remarks>
    /// <returns>A <see cref="string"/> object.</returns>
    public override string ToString()
    {
        if (EnableRateLimiting == false)
        {
            return string.Empty;
        }

        char hdrSign = EnableHeaders == false ? '-' : '+';
        string waitWindow = PeriodTimespan.HasValue ? PeriodTimespan.Value.ToString("F3") + 's' : Wait.IfEmpty(None);
        return $"H{hdrSign}:{Limit}:{Period}:w{waitWindow}";
    }

    public const string None = "-";
}
