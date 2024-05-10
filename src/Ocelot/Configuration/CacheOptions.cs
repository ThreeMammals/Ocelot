using Ocelot.Request.Middleware;

namespace Ocelot.Configuration
{
    public class CacheOptions
    {
        internal CacheOptions() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheOptions"/> class.
        /// The default value for EnableContentHashing is false, but
        /// it is set to null for route-level configuration to allow
        /// global configuration usage.
        /// The default value for TtlSeconds is 0
        /// </summary>
        /// <param name="ttlSeconds"></param>
        /// <param name="region"></param>
        /// <param name="header"></param>
        /// <param name="enableContentHashing"></param>
        public CacheOptions(int? ttlSeconds, string region, string header, bool? enableContentHashing)
        {
            TtlSeconds = ttlSeconds ?? 0;
            Region = region;
            Header = header;
            EnableContentHashing = enableContentHashing ?? false;
        }

        public int TtlSeconds { get; }
        public string Region { get; }
        public string Header { get; }

        /// <summary>
        /// Enables MD5 hash calculation of the <see cref="HttpRequestMessage.Content"/> of the <see cref="DownstreamRequest.Request"/> object.
        /// </summary>
        /// <remarks>
        /// Default value is <see langword="false"/>. No hashing by default.
        /// </remarks>
        /// <value>
        /// <see langword="true"/> if hashing is enabled, otherwise it is <see langword="false"/>.
        /// </value>
        public bool EnableContentHashing { get; }
    }
}
