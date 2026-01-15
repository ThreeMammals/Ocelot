using System.Globalization;
using System.Text.Json.Serialization;
using NewtonsoftJsonConstructor = Newtonsoft.Json.JsonConstructorAttribute;

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
    [NewtonsoftJsonConstructor]
    public RateLimitCounter(DateTime startedAt, DateTime? exceededAt, long totalRequests)
    {
        StartedAt = startedAt;
        ExceededAt = exceededAt;
        Total = totalRequests;
    }

    /// <summary>The moment when the counting was started.</summary>
    /// <value>A <see cref="DateTime"/> value of the moment.</value>
    public DateTime StartedAt { get; }

    /// <summary>The moment when the limit was exceeded.</summary>
    /// <value>A <see cref="DateTime"/> value of the moment.</value>
    public DateTime? ExceededAt;

    /// <summary>Total number of requests counted.</summary>
    /// <value>A <see langword="long"/> value of total number.</value>
    public long Total;

    public override readonly string ToString()
    {
        string started = StartedAt.ToString("O", CultureInfo.InvariantCulture);
        string exceeded = ExceededAt.HasValue
            ? $"+{ExceededAt.Value - StartedAt}"
            : string.Empty;
        return $"{Total}->({started}){exceeded}";
    }
}
