using Ocelot.Request.Middleware;

namespace Ocelot.Configuration
{
    public class CacheOptions
    {
        internal CacheOptions() { }

        public CacheOptions(int ttlSeconds, string region, string header)
        {
            TtlSeconds = ttlSeconds;
            Region = region;
            Header = header;
        }

        public CacheOptions(int ttlSeconds, string region, string header, bool enableContentHashing)
        {
            TtlSeconds = ttlSeconds;
            Region = region;
            Header = header;
            EnableContentHashing = enableContentHashing;
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
