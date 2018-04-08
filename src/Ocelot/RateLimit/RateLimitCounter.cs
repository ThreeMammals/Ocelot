using System;

namespace Ocelot.RateLimit
{
    /// <summary>
    /// Stores the initial access time and the numbers of calls made from that point
    /// </summary>
    public struct RateLimitCounter
    {
        public RateLimitCounter(DateTime timestamp, long totalRequest)
        {
            Timestamp = timestamp;
            TotalRequests = totalRequest;
        }

        public DateTime Timestamp { get; private set; }

        public long TotalRequests { get; private set; }
    }
}
