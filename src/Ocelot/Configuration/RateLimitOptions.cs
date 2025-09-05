using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using Ocelot.RateLimiting;

namespace Ocelot.Configuration;

/// <summary>
/// RateLimit Options.
/// </summary>
public class RateLimitOptions
{
    public const string DefaultClientHeader = "Oc-Client";
    public static readonly string DefaultCounterPrefix = typeof(RateLimiting.RateLimiting).Namespace;
    public const int DefaultStatus429 = StatusCodes.Status429TooManyRequests;
    public const string DefaultQuotaMessage = "API calls quota exceeded! Maximum admitted {0} per {1}.";

    public RateLimitOptions()
    {
        ClientIdHeader = DefaultClientHeader;
        ClientWhitelist = [];
        EnableHeaders = true;
        EnableRateLimiting = true;
        StatusCode = DefaultStatus429;
        QuotaMessage = DefaultQuotaMessage;
        KeyPrefix = DefaultCounterPrefix;
        Rule = RateLimitRule.Empty;
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
        StatusCode = httpStatusCode;
        QuotaMessage = quotaExceededMessage.IfEmpty(DefaultQuotaMessage);
        KeyPrefix = rateLimitCounterPrefix.IfEmpty(DefaultCounterPrefix);
        Rule = rateLimitRule;
    }

    public RateLimitOptions(FileRateLimitByHeaderRule from)
    {
        ArgumentNullException.ThrowIfNull(from);

        ClientIdHeader = from.ClientIdHeader.IfEmpty(DefaultClientHeader);
        ClientWhitelist = from.ClientWhitelist ?? [];
        EnableHeaders = from.DisableRateLimitHeaders.HasValue ? !from.DisableRateLimitHeaders.Value
            : from.EnableHeaders ?? true;
        EnableRateLimiting = from.EnableRateLimiting ?? true;
        StatusCode = from.HttpStatusCode ?? from.StatusCode ?? DefaultStatus429;
        QuotaMessage = from.QuotaExceededMessage.IfEmpty(from.QuotaMessage.IfEmpty(DefaultQuotaMessage));
        KeyPrefix = from.RateLimitCounterPrefix.IfEmpty(from.KeyPrefix.IfEmpty(DefaultCounterPrefix));
        Rule = new(
            from.Period.IfEmpty(RateLimitRule.DefaultPeriod),
            from.PeriodTimespan.HasValue ? $"{from.PeriodTimespan.Value}s" : from.Wait,
            from.Limit ?? RateLimitRule.ZeroLimit);
    }

    /// <summary>
    /// Gets a Rate Limit rule.
    /// </summary>
    /// <value>
    /// A <see cref="RateLimitRule"/> object that represents the rule.
    /// </value>
    public RateLimitRule Rule { get; init; }

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
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets or sets a value to be used as the formatter for the Quota Exceeded response message.
    /// <para>If none specified the default will be: <see cref="DefaultQuotaMessage"/>.</para>
    /// </summary>
    /// <value>A <see cref="string"/> value that will be used as a formatter.</value>
    public string QuotaMessage { get; init; }

    /// <summary>Gets or sets the counter prefix, used to compose the rate limiting counter caching key to be used by the <see cref="IRateLimitStorage"/> service.</summary>
    /// <remarks>Notes:
    /// <list type="number">
    /// <item>The consumer is the <see cref="IRateLimiting.GetStorageKey(ClientRequestIdentity, RateLimitOptions)"/> method.</item>
    /// <item>The property is relevant for distributed storage systems, such as <see cref="IDistributedCache"/> services, to inform users about which objects are being cached for management purposes.
    /// By default, each Ocelot instance uses its own <see cref="IMemoryCache"/> service without cross-instance synchronization.</item>
    /// </list>
    /// </remarks>
    /// <value>A <see cref="string"/> object which value defaults to "Ocelot.RateLimiting", see the <see cref="DefaultCounterPrefix"/> property.</value>
    public string KeyPrefix { get; init; }

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
