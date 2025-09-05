using Ocelot.Infrastructure.Extensions;
using Ocelot.RateLimiting;

namespace Ocelot.Configuration.File;

public class FileRateLimitByHeaderRule : FileRateLimitRule
{
    public FileRateLimitByHeaderRule() : base()
    { }

    public FileRateLimitByHeaderRule(FileRateLimitRule from)
        : base(from)
    {
        ClientWhitelist = default;
    }

    public FileRateLimitByHeaderRule(FileRateLimitByHeaderRule from)
        : base(from)
    {
        ClientIdHeader = from.ClientIdHeader;
        ClientWhitelist = from.ClientWhitelist;
        DisableRateLimitHeaders = from.DisableRateLimitHeaders;
        HttpStatusCode = from.HttpStatusCode;
        QuotaExceededMessage = from.QuotaExceededMessage;
        RateLimitCounterPrefix = from.RateLimitCounterPrefix;
    }

    /// <summary>Gets or sets the HTTP header used to store the client identifier, which defaults to <c>Oc-Client</c>.</summary>
    /// <value>A <see cref="string"/> representing the name of the HTTP header.</value>
    public string ClientIdHeader { get; set; }

    /// <summary>A list of approved clients aka whitelisted ones.</summary>
    /// <value>An <see cref="IList{T}"/> collection of allowed clients.</value>
    public IList<string> ClientWhitelist { get; set; }

    /// <summary>
    /// Returns a string that represents the current rule in the format, which defaults to empty string if rate limiting is disabled (<see cref="FileRateLimitRule.EnableRateLimiting"/> is <see langword="false"/>).
    /// </summary>
    /// <remarks>Format: <c>H{+,-}:{limit}:{period}:w{wait}/HDR:{client_id_header}/WL[{c1,c2,...}]</c>.</remarks>
    /// <returns>A <see cref="string"/> object.</returns>
    public override string ToString() => EnableRateLimiting == false ? string.Empty
        : (DisableRateLimitHeaders.HasValue ? $"H{(DisableRateLimitHeaders == true ? '-' : '+')}" + base.ToString().TrimStart('H', '+', '-') : base.ToString())
            + $"/HDR:{ClientIdHeader.IfEmpty(None)}/WL{(ClientWhitelist is null ? None : '[' + string.Join(',', ClientWhitelist) + ']')}";

    /// <summary>Disables or enables <c>X-Rate-Limit-*</c> and <c>Retry-After</c> headers.</summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see cref="bool"/>.</value>
    [Obsolete("Use EnableHeaders instead of DisableRateLimitHeaders! Note that DisableRateLimitHeaders will be removed in version 25.0!")]
    public bool? DisableRateLimitHeaders { get; set; }

    /// <summary>Gets or sets the rejection status code returned during the Quota Exceeded period, aka the <see cref="FileRateLimitRule.PeriodTimespan"/> wait window, or the remainder of the <see cref="FileRateLimitRule.Period"/> fixed window following the moment of exceeding.
    /// <para>Default value: <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/429">429 (Too Many Requests)</see>.</para></summary>
    /// <value>A <see cref="Nullable{T}"/> value, where <c>T</c> is <see cref="int"/>.</value>
    [Obsolete("Use StatusCode instead of HttpStatusCode! Note that HttpStatusCode will be removed in version 25.0!")]
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Gets or sets a value to be used as the formatter for the Quota Exceeded response message.
    /// <para>If none specified the default will be: <see cref="RateLimitOptions.DefaultQuotaMessage"/>.</para>
    /// </summary>
    /// <value>A <see cref="string"/> value that will be used as a formatter.</value>
    [Obsolete("Use QuotaMessage instead of QuotaExceededMessage! Note that QuotaExceededMessage will be removed in version 25.0!")]
    public string QuotaExceededMessage { get; set; }

    /// <summary>Gets or sets the counter prefix, used to compose the rate limit counter cache key to be used by the <see cref="IRateLimitStorage"/> service.</summary>
    /// <remarks>The consumer is the <see cref="IRateLimiting.GetStorageKey(ClientRequestIdentity, RateLimitOptions)"/> method.</remarks>
    /// <value>A <see cref="string"/> object which value defaults to "ocelot".</value>
    public string RateLimitCounterPrefix { get; set; }
}
