using System.Globalization;
using System.Text.Json.Serialization;

namespace Ocelot.RateLimiting;

/// <summary>
/// Stores the initial access time and the numbers of calls made from that point.
/// </summary>
public struct RateLimitCounter
{
    public RateLimitCounter(DateTime startedAt)
    {
        StartedAt = startedAt;
        Total = 1;
    }

    [JsonConstructor]
    public RateLimitCounter(DateTime startedAt, DateTime? exceededAt, long total)
    {
        StartedAt = startedAt;
        ExceededAt = exceededAt;
        Total = total;
    }

    /// <summary>The moment when the counting was started.</summary>
    /// <value>A <see cref="DateTime"/> value of the moment.</value>
    public DateTime StartedAt { get; }

    /// <summary>The moment when the limit was exceeded.</summary>
    /// <value>A <see cref="DateTime"/> value of the moment.</value>
    public DateTime? ExceededAt { get; set; }

    /// <summary>Total number of requests counted.</summary>
    /// <value>A <see langword="long"/> value of total number.</value>
    public long Total { get; set; }

    public override readonly string ToString()
    {
        string started = StartedAt.ToString("O", CultureInfo.InvariantCulture);
        string exceeded = ExceededAt.HasValue
            ? $"+{ExceededAt.Value - StartedAt}"
            : string.Empty;
        return $"{Total}->({started}){exceeded}";
    }
}
