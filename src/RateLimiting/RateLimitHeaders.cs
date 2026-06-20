using Microsoft.AspNetCore.Http;

namespace Ocelot.RateLimiting;

public class RateLimitHeaders
{
    protected RateLimitHeaders() { }

    public RateLimitHeaders(HttpContext context, long limit, long remaining, DateTime reset)
    {
        Context = context;
        Limit = limit;
        Remaining = remaining;
        Reset = reset;
    }

    /// <summary>
    /// Original context.
    /// </summary>
    /// <value>An <see cref="HttpContext"/> object.</value>
    public HttpContext Context { get; }

    /// <summary>
    /// Total number of requests allowed in the current time window.
    /// </summary>
    /// <value>An <see cref="long"/> value.</value>
    public long Limit { get; }

    /// <summary>
    /// Number of requests remaining before hitting the limit.
    /// </summary>
    /// <value>An <see cref="long"/> value.</value>
    public long Remaining { get; }

    /// <summary>
    /// Timestamp when the rate limit window resets.
    /// </summary>
    /// <value>A <see cref="DateTime"/> value.</value>
    public DateTime Reset { get; }

    public override string ToString() => $"{Remaining}/{Limit} resets at {Reset:O}";
}
