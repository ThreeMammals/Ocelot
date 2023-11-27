namespace Ocelot.Configuration.File
{
    public class FileCacheOptions
    {
        public FileCacheOptions()
        {
            Region = string.Empty;
            TtlSeconds = 0;
        }

        public FileCacheOptions(FileCacheOptions from)
        {
            Region = from.Region;
            TtlSeconds = from.TtlSeconds;
        }

        public string Region { get; set; }
        public int TtlSeconds { get; set; }
    }
}
