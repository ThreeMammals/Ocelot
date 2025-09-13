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
        KeyPrefix = rateLimitCounterPrefix.IfEmpty(DefaultCounterPrefix);
        QuotaMessage = quotaExceededMessage.IfEmpty(DefaultQuotaMessage);
        Rule = rateLimitRule;
        StatusCode = httpStatusCode;
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

    public RateLimitOptions(RateLimitOptions from)
    {
        ArgumentNullException.ThrowIfNull(from);

        ClientIdHeader = from.ClientIdHeader.IfEmpty(DefaultClientHeader);
        ClientWhitelist = from.ClientWhitelist ?? [];
        EnableHeaders = from.EnableHeaders;
        EnableRateLimiting = from.EnableRateLimiting;
        StatusCode = from.StatusCode;
        QuotaMessage = from.QuotaMessage.IfEmpty(DefaultQuotaMessage);
        KeyPrefix = from.KeyPrefix.IfEmpty(DefaultCounterPrefix);
        Rule = from.Rule ?? RateLimitRule.Empty;
    }

    /// <summary>Gets a Rate Limit rule.</summary>
    /// <value>A <see cref="RateLimitRule"/> object that represents the rule.</value>
    public RateLimitRule Rule { get; init; }

    /// <summary>A list of approved clients aka whitelisted ones.</summary>
    /// <value>An <see cref="IList{T}"/> collection of allowed clients.</value>
    public IList<string> ClientWhitelist { get; init; }

    /// <summary>Gets or sets the HTTP header used to store the client identifier, which defaults to <c>Oc-Client</c>.</summary>
    /// <value>A <see cref="string"/> representing the name of the HTTP header.</value>
    public string ClientIdHeader { get; init; }

    /// <summary>Gets or sets the rejection status code returned during the Quota Exceeded period, aka the <see cref="Wait"/> window, or the remainder of the <see cref="Period"/> fixed window following the moment of exceeding.
    /// <para>Default value: <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/429">429 (Too Many Requests)</see>.</para></summary>
    /// <value>A <see cref="int"/> value.</value>
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

    /// <summary>Enables or disables rate limiting. Defaults to <see langword="true"/> (enabled).</summary>
    /// <value>A <see langword="bool"/> value.</value>
    public bool EnableRateLimiting { get; init; }

    /// <summary>Enables or disables <c>X-RateLimit-*</c> and <c>Retry-After</c> headers.</summary>
    /// <value>A <see cref="bool"/> value.</value>
    public bool EnableHeaders { get; init; }
}
