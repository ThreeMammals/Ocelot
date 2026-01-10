using Microsoft.Net.Http.Headers;

namespace Ocelot.RateLimiting;

/// <summary>
/// TODO These Ocelot's RateLimiting headers don't follow industry standards, see links.
/// </summary>
/// <remarks>Links:
/// <list type="bullet">
/// <item>GitHub: <see href="https://github.com/ioggstream/draft-polli-ratelimit-headers">draft-polli-ratelimit-headers</see></item>
/// <item>GitHub: <see href="https://github.com/ietf-wg-httpapi/ratelimit-headers">ratelimit-headers</see></item>
/// <item>GitHub Wiki: <see href="https://ietf-wg-httpapi.github.io/ratelimit-headers/draft-ietf-httpapi-ratelimit-headers.html">RateLimit header fields for HTTP</see></item>
/// <item>StackOverflow: <see href="https://stackoverflow.com/questions/16022624/examples-of-http-api-rate-limiting-http-response-headers">Examples of HTTP API Rate Limiting HTTP Response headers</see></item>
/// </list>
/// </remarks>
public static class RateLimitingHeaders
{
    public const char Dash = '-';
    public const char Underscore = '_';

    /// <summary>Gets the <c>Retry-After</c> HTTP header name.</summary>
    public static readonly string Retry_After = HeaderNames.RetryAfter;

    /// <summary>Gets the <c>X-RateLimit-Limit</c> Ocelot's header name.</summary>
    public static readonly string X_RateLimit_Limit = nameof(X_RateLimit_Limit).Replace(Underscore, Dash);

    /// <summary>Gets the <c>X-RateLimit-Remaining</c> Ocelot's header name.</summary>
    public static readonly string X_RateLimit_Remaining = nameof(X_RateLimit_Remaining).Replace(Underscore, Dash);

    /// <summary>Gets the <c>X-RateLimit-Reset</c> Ocelot's header name.</summary>
    public static readonly string X_RateLimit_Reset = nameof(X_RateLimit_Reset).Replace(Underscore, Dash);
}
