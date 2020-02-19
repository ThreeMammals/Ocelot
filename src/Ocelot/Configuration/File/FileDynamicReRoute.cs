namespace Ocelot.Configuration.File
{
    public class FileDynamicReRoute
    {
        public string ServiceName { get; set; }
        public FileRateLimitRule RateLimitRule { get; set; }
        public string DownstreamHttpVersion { get; set; }
    }
}
