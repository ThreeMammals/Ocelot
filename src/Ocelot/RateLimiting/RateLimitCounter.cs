using Newtonsoft.Json;

namespace Ocelot.RateLimiting;

/// <summary>
/// Stores the initial access time and the numbers of calls made from that point.
/// </summary>
public struct RateLimitCounter
{
    [JsonConstructor]
    public RateLimitCounter(DateTime startedAt, DateTime? exceededAt, long totalRequests)
    {
        StartedAt = startedAt;
        ExceededAt = exceededAt;
        TotalRequests = totalRequests;
    }

    /// <summary>The moment when the counting was started.</summary>
    /// <value>A <see cref="DateTime"/> value of the moment.</value>
    public DateTime StartedAt { get; }

    /// <summary>The moment when the limit was exceeded.</summary>
    /// <value>A <see cref="DateTime"/> value of the moment.</value>
    public DateTime? ExceededAt { get; }

    /// <summary>Total number of requests counted.</summary>
    /// <value>A <see langword="long"/> value of total number.</value>
    public long TotalRequests { get; set; }
}
