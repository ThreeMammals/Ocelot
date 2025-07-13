using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;

namespace Ocelot.RateLimiting;

/// <summary>
/// Defines basic Rate Limiting functionality.
/// </summary>
public interface IRateLimiting
{
    /// <summary>Retrieves the key for the attached storage.</summary>
    /// <remarks>See the <see cref="IRateLimitStorage"/> interface.</remarks>
    /// <param name="identity">The current representation of the request.</param>
    /// <param name="options">The options of rate limiting.</param>
    /// <returns>A <see langword="string"/> value of the key.</returns>
    string GetStorageKey(ClientRequestIdentity identity, RateLimitOptions options);

    /// <summary>
    /// Gets required information to create wanted headers in upper contexts (middleware, etc).
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <param name="identity">The current representation of the request.</param>
    /// <param name="options">The options of rate limiting.</param>
    /// <returns>A <see cref="RateLimitHeaders"/> value.</returns>
    RateLimitHeaders GetHeaders(HttpContext context, ClientRequestIdentity identity, RateLimitOptions options);

    /// <summary>
    /// Main entry point to process the current request and apply the limiting rule.
    /// </summary>
    /// <remarks>Warning! The method performs the storage operations which should be thread safe.</remarks>
    /// <param name="identity">The representation of current request.</param>
    /// <param name="options">The current rate limiting options.</param>
    /// <returns>A <see cref="RateLimitCounter"/> value.</returns>
    RateLimitCounter ProcessRequest(ClientRequestIdentity identity, RateLimitOptions options);

    /// <summary>
    /// Counts requests based on the current counter state and taking into account the limiting rule.
    /// </summary>
    /// <param name="entry">Old counter with starting moment inside.</param>
    /// <param name="rule">The limiting rule.</param>
    /// <returns>A <see cref="RateLimitCounter"/> value.</returns>
    RateLimitCounter Count(RateLimitCounter? entry, RateLimitRule rule);

    /// <summary>
    /// Gets the seconds to wait for the next retry by starting moment and the rule.
    /// </summary>
    /// <remarks>The method must be called after the counting by the <see cref="Count(RateLimitCounter?, RateLimitRule)"/> method is completed; otherwise it doesn't make sense.</remarks>
    /// <param name="counter">The counter with starting moment inside.</param>
    /// <param name="rule">The limiting rule.</param>
    /// <returns>A <see cref="double"/> value in seconds.</returns>
    double RetryAfter(RateLimitCounter counter, RateLimitRule rule);

    /// <summary>
    /// Converts to time span from a string, such as "1s", "1m", "1h", "1d".
    /// </summary>
    /// <param name="timespan">The string value with dimentions: '1s', '1m', '1h', '1d'.</param>
    /// <returns>A <see cref="TimeSpan"/> value.</returns>
    TimeSpan ToTimespan(string timespan);
}
