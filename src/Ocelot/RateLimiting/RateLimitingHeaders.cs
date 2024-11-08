﻿using Microsoft.Net.Http.Headers;

namespace Ocelot.RateLimiting;

/// <summary>
/// TODO These Ocelot's RateLimiting headers don't follow industry standards, see links.
/// </summary>
/// <remarks>Links:
/// <list type="bullet">
/// <item>GitHub: <see href="https://github.com/ioggstream/draft-polli-ratelimit-headers">draft-polli-ratelimit-headers</see></item>
/// <item>GitHub: <see href="https://github.com/ietf-wg-httpapi/ratelimit-headers">ratelimit-headers</see></item>
/// <item>GitHub Wiki: <see href="https://ietf-wg-httpapi.github.io/ratelimit-headers/draft-ietf-httpapi-ratelimit-headers.html">RateLimit header fields for HTTP </see></item>
/// </list>
/// </remarks>
public static class RateLimitingHeaders
{
    /// <summary>Gets the <c>Retry-After</c> HTTP header name.</summary>
    public static readonly string RetryAfter = HeaderNames.RetryAfter;

    /// <summary>Gets the <c>X-Rate-Limit-Limit</c> Ocelot's header name.</summary>
    public static readonly string X_Rate_Limit_Limit = "X-Rate-Limit-Limit";

    /// <summary>Gets the <c>X-Rate-Limit-Remaining</c> Ocelot's header name.</summary>
    public static readonly string X_Rate_Limit_Remaining = "X-Rate-Limit-Remaining";

    /// <summary>Gets the <c>X-Rate-Limit-Reset</c> Ocelot's header name.</summary>
    public static readonly string X_Rate_Limit_Reset = "X-Rate-Limit-Reset";
}
