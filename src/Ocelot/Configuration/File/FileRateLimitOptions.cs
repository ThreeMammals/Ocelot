namespace Ocelot.Configuration.File
{
    public class FileRateLimitOptions
    {
        /// <summary>
        /// Gets or sets the HTTP header that holds the client identifier, by default is X-ClientId.
        /// </summary>
        /// <value>
        /// A string with the HTTP header that holds the client identifier, by default is X-ClientId.
        /// </value>
        public string ClientIdHeader { get; set; } = "ClientId";

        /// <summary>
        /// Gets or sets a value that will be used as a formatter for the QuotaExceeded response message.
        /// If none specified the default will be:
        /// API calls quota exceeded! maximum admitted {0} per {1}.
        /// </summary>
        /// <value>
        /// A string value that will be used as a formatter.
        /// <para>
        /// If none specified the default will be: "API calls quota exceeded! maximum admitted {0} per {1}".
        /// </para>
        /// </value>
        public string QuotaExceededMessage { get; set; }

        /// <summary>
        /// Gets or sets the counter prefix, used to compose the rate limit counter cache key.
        /// </summary>
        /// <value>
        /// A string with counter prefix, used to compose the rate limit counter cache key.
        /// </value>
        public string RateLimitCounterPrefix { get; set; } = "ocelot";

        /// <summary>
        /// Disables X-Rate-Limit and Rety-After headers.
        /// </summary>
        /// <value>
        /// A boolean value for disabling X-Rate-Limit and Rety-After headers.
        /// </value>
        public bool DisableRateLimitHeaders { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Status code returned when rate limiting occurs, by default value is set to 429 (Too Many Requests).
        /// </summary>
        /// <value>
        /// An integer value with the HTTP Status code returned when rate limiting occurs.
        /// <para>
        /// Default value: 429 (Too Many Requests).
        /// </para>
        /// </value>
        public int HttpStatusCode { get; set; } = 429;
    }
}
