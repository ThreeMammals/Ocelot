namespace Ocelot.Configuration.File
{
    public class FileCacheOptions
    {
        public FileCacheOptions()
        {
            Header = string.Empty;
            Region = string.Empty;
            TtlSeconds = 0;
        }

        public FileCacheOptions(FileCacheOptions from)
        {
            Header = from.Header;
            Region = from.Region;
            TtlSeconds = from.TtlSeconds;
        }

        public string Header { get; set; }
        public string Region { get; set; }
        public int TtlSeconds { get; set; }
    }
}
