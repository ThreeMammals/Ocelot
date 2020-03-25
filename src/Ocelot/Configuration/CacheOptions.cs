namespace Ocelot.Configuration
{
    public class CacheOptions
    {
        public CacheOptions(int ttlSeconds, string region, string header)
        {
            TtlSeconds = ttlSeconds;
            Region = region;
            Header = header;
        }

        public int TtlSeconds { get; }

        public string Region { get; }

        public string Header { get; }
    }
}
