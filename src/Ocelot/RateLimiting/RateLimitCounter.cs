using Newtonsoft.Json;

namespace Ocelot.RateLimiting;

/// <summary>
/// Stores the initial access time and the numbers of calls made from that point.
/// </summary>
public readonly struct RateLimitCounter
{
    [JsonConstructor]
    public RateLimitCounter(DateTime timestamp, long totalRequests)
    {
        Timestamp = timestamp;
        TotalRequests = totalRequests;
    }

    /// <summary>The moment when the counting was started.</summary>
    /// <value>A <see cref="DateTime"/> value of the moment.</value>
    public DateTime Timestamp { get; }

    /// <summary>Total number of requests counted.</summary>
    /// <value>A <see langword="long"/> value of total number.</value>
    public long TotalRequests { get; }
}
