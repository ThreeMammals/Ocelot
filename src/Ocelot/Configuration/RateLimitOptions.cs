namespace Ocelot.Configuration
{
    /// <summary>
    /// RateLimit Options.
    /// </summary>
    public class RateLimitOptions
    {
        private readonly Func<List<string>> _getClientWhitelist;

        public RateLimitOptions(bool enableRateLimiting, string clientIdHeader, Func<List<string>> getClientWhitelist, bool disableRateLimitHeaders,
            string quotaExceededMessage, string rateLimitCounterPrefix, RateLimitRule rateLimitRule, int httpStatusCode)
        {
            EnableRateLimiting = enableRateLimiting;
            ClientIdHeader = clientIdHeader;
            _getClientWhitelist = getClientWhitelist;
            DisableRateLimitHeaders = disableRateLimitHeaders;
            QuotaExceededMessage = quotaExceededMessage;
            RateLimitCounterPrefix = rateLimitCounterPrefix;
            RateLimitRule = rateLimitRule;
            HttpStatusCode = httpStatusCode;
        }

        /// <summary>
        /// Gets a Rate Limit rule.
        /// </summary>
        /// <value>
        /// A <see cref="Configuration.RateLimitRule"/> object that represents the rule.
        /// </value>
        public RateLimitRule RateLimitRule { get; }

        /// <summary>
        /// Gets the list of white listed clients.
        /// </summary>
        /// <value>
        /// A <see cref="List{T}"/> collection with white listed clients.
        /// </value>
        public List<string> ClientWhitelist => _getClientWhitelist();

        /// <summary>
        /// Gets or sets the HTTP header that holds the client identifier, by default is X-ClientId.
        /// </summary>
        /// <value>
        /// A string value with the HTTP header.
        /// </value>
        public string ClientIdHeader { get; }

        /// <summary>
        /// Gets or sets the HTTP Status code returned when rate limiting occurs, by default value is set to 429 (Too Many Requests).
        /// </summary>
        /// <value>
        /// An integer value with the HTTP Status code.
        /// <para>Default value: 429 (Too Many Requests).</para>
        /// </value>
        public int HttpStatusCode { get; }

        /// <summary>
        /// Gets or sets a value that will be used as a formatter for the QuotaExceeded response message.
        /// <para>If none specified the default will be: "API calls quota exceeded! maximum admitted {0} per {1}".</para>
        /// </summary>
        /// <value>
        /// A string value with a formatter for the QuotaExceeded response message.
        /// <para>Default will be: "API calls quota exceeded! maximum admitted {0} per {1}".</para>
        /// </value>
        public string QuotaExceededMessage { get; }

        /// <summary>
        /// Gets or sets the counter prefix, used to compose the rate limit counter cache key.
        /// </summary>
        /// <value>
        /// A string value with the counter prefix.
        /// </value>
        public string RateLimitCounterPrefix { get; }

        /// <summary>
        /// Enables endpoint rate limiting based URL path and HTTP verb.
        /// </summary>
        /// <value>
        /// A boolean value for enabling endpoint rate limiting based URL path and HTTP verb.
        /// </value>
        public bool EnableRateLimiting { get; }

        /// <summary>
        /// Disables X-Rate-Limit and Rety-After headers.
        /// </summary>
        /// <value>
        /// A boolean value for disabling X-Rate-Limit and Rety-After headers.
        /// </value>
        public bool DisableRateLimitHeaders { get; }
    }
}
