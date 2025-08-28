using Microsoft.AspNetCore.Http;

namespace Ocelot.Configuration.File;

public class FileRateLimitOptions : IRouteGroup
{
    /// <summary>Gets or sets the HTTP header that holds the client identifier, by default is X-ClientId.</summary>
    /// <value>A string with the HTTP header that holds the client identifier, by default is X-ClientId.</value>
    public string ClientIdHeader { get; set; } = "ClientId";
    public IList<string> ClientWhitelist { get; set; }

    /// <summary>Disables X-Rate-Limit and Rety-After headers.</summary>
    /// <value>A boolean value for disabling X-Rate-Limit and Rety-After headers.</value>
    [Obsolete("Use EnableHeaders instead of DisableRateLimitHeaders! Note that DisableRateLimitHeaders will be removed in version 25.0.")]
    public bool? DisableRateLimitHeaders { get; set; }
    public bool EnableHeaders { get; set; } = true; // defaults to 'enabled' via def ctor

    /// <summary>Gets or sets the HTTP Status code returned when rate limiting occurs, by default value is set to 429 (Too Many Requests).
    /// <para>Default value: 429 (Too Many Requests).</para></summary>
    /// <value>An integer value with the HTTP Status code returned when rate limiting occurs.</value>
    public int HttpStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;

    /// <summary>
    /// Gets or sets a value that will be used as a formatter for the QuotaExceeded response message.
    /// If none specified the default will be:
    /// API calls quota exceeded! maximum admitted {0} per {1}.
    /// <para>If none specified the default will be: "API calls quota exceeded! maximum admitted {0} per {1}".</para>
    /// </summary>
    /// <value>A string value that will be used as a formatter.</value>
    public string QuotaExceededMessage { get; set; }

    /// <summary>Gets or sets the counter prefix, used to compose the rate limit counter cache key.</summary>
    /// <value>A string with counter prefix, used to compose the rate limit counter cache key.</value>
    public string RateLimitCounterPrefix { get; set; } = "ocelot";

    /// <summary>Gets or sets the keys used to group routes, based on the already defined <see cref="FileRoute.Key"/> property.</summary>
    /// <remarks>If not empty, these options are applied specifically to the route with those keys; otherwise, they are applied to all routes.</remarks>
    /// <value>An <see cref="IList{T}"/> collection of keys that determine which routes the options should be applied to.</value>
    public IList<string> RouteKeys { get; set ; }
}
