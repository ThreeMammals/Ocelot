using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration;

/// <summary>
/// RateLimit Options.
/// </summary>
public class RateLimitOptions
{
    public const string DefaultClientHeader = "Oc-Client";
    public const string DefaultCounterPrefix = "ocelot";
    public const int DefaultStatus429 = StatusCodes.Status429TooManyRequests;
    public const string DefaultQuotaMessage = "API calls quota exceeded! Maximum admitted {0} per {1}.";

    public RateLimitOptions()
    {
        ClientIdHeader = DefaultClientHeader;
        ClientWhitelist = [];
        EnableHeaders = true;
        EnableRateLimiting = true;
        HttpStatusCode = DefaultStatus429;
        QuotaExceededMessage = DefaultQuotaMessage;
        RateLimitCounterPrefix = DefaultCounterPrefix;
        RateLimitRule = RateLimitRule.Empty;
    }

    public RateLimitOptions(bool enableRateLimiting) : this()
    {
        EnableRateLimiting = enableRateLimiting;
    }

    public RateLimitOptions(bool enableRateLimiting, string clientIdHeader, IList<string> clientWhitelist, bool enableHeaders,
        string quotaExceededMessage, string rateLimitCounterPrefix, RateLimitRule rateLimitRule, int httpStatusCode)
    {
        ClientIdHeader = clientIdHeader.IfEmpty(DefaultClientHeader);
        ClientWhitelist = clientWhitelist ?? [];
        EnableHeaders = enableHeaders;
        EnableRateLimiting = enableRateLimiting;
        HttpStatusCode = httpStatusCode;
        QuotaExceededMessage = quotaExceededMessage.IfEmpty(DefaultQuotaMessage);
        RateLimitCounterPrefix = rateLimitCounterPrefix.IfEmpty(DefaultCounterPrefix);
        RateLimitRule = rateLimitRule;
    }

    public RateLimitOptions(FileRateLimitByHeaderRule from)
    {
        ArgumentNullException.ThrowIfNull(from);

        ClientIdHeader = from.ClientIdHeader.IfEmpty(DefaultClientHeader);
        ClientWhitelist = from.ClientWhitelist ?? [];
        EnableHeaders = from.DisableRateLimitHeaders.HasValue ? !from.DisableRateLimitHeaders.Value
            : from.EnableHeaders ?? true;
        EnableRateLimiting = from.EnableRateLimiting ?? true;
        HttpStatusCode = from.HttpStatusCode ?? DefaultStatus429;
        QuotaExceededMessage = from.QuotaExceededMessage.IfEmpty(DefaultQuotaMessage);
        RateLimitCounterPrefix = from.RateLimitCounterPrefix.IfEmpty(DefaultCounterPrefix);
        RateLimitRule = new(
            from.Period.IfEmpty(RateLimitRule.DefaultPeriod),
            from.PeriodTimespan ?? RateLimitRule.ZeroPeriodTimespan,
            from.Limit ?? RateLimitRule.ZeroLimit);
    }

    /// <summary>
    /// Gets a Rate Limit rule.
    /// </summary>
    /// <value>
    /// A <see cref="Configuration.RateLimitRule"/> object that represents the rule.
    /// </value>
    public RateLimitRule RateLimitRule { get; init; }

    /// <summary>
    /// Gets the list of white listed clients.
    /// </summary>
    /// <value>
    /// A <see cref="IList{T}"/> (where T is <see cref="string"/>) collection with white listed clients.
    /// </value>
    public IList<string> ClientWhitelist { get; init; }

    /// <summary>
    /// Gets or sets the HTTP header that holds the client identifier, by default is X-ClientId.
    /// </summary>
    /// <value>
    /// A string value with the HTTP header.
    /// </value>
    public string ClientIdHeader { get; init; }

    /// <summary>
    /// Gets or sets the HTTP Status code returned when rate limiting occurs, by default value is set to 429 (Too Many Requests).
    /// </summary>
    /// <value>
    /// An integer value with the HTTP Status code.
    /// <para>Default value: 429 (Too Many Requests).</para>
    /// </value>
    public int HttpStatusCode { get; init; }

    /// <summary>
    /// Gets or sets a value that will be used as a formatter for the QuotaExceeded response message.
    /// <para>If none specified the default will be: "API calls quota exceeded! maximum admitted {0} per {1}".</para>
    /// </summary>
    /// <value>
    /// A string value with a formatter for the QuotaExceeded response message.
    /// <para>Default will be: "API calls quota exceeded! maximum admitted {0} per {1}".</para>
    /// </value>
    public string QuotaExceededMessage { get; init; }

    /// <summary>
    /// Gets or sets the counter prefix, used to compose the rate limit counter cache key.
    /// </summary>
    /// <value>
    /// A string value with the counter prefix.
    /// </value>
    public string RateLimitCounterPrefix { get; init; }

    /// <summary>
    /// Enables endpoint rate limiting based URL path and HTTP verb.
    /// </summary>
    /// <value>
    /// A boolean value for enabling endpoint rate limiting based URL path and HTTP verb.
    /// </value>
    public bool EnableRateLimiting { get; init; }

    /// <summary>Enables or disables <c>X-Rate-Limit</c> and <c>Retry-After</c> headers.</summary>
    /// <value>A <see cref="bool"/> value.</value>
    public bool EnableHeaders { get; init; }
}
