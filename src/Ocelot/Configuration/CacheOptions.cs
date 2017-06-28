namespace Ocelot.Configuration
{
    public class CacheOptions
    {
        public CacheOptions(int ttlSeconds)
        {
            TtlSeconds = ttlSeconds;
        }

        public int TtlSeconds { get; private set; }
        public string Region {get;private set;}
    }
}
