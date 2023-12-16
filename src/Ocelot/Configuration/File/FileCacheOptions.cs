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

        public int TtlSeconds { get; set; }
        public string Region { get; set; }
        public string Header { get; set; }
    }
}
