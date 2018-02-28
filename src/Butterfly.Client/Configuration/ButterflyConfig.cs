namespace Butterfly.Client
{
    public class ButterflyConfig
    {
        public string Service { get; set; }

        public string ServiceIdentity { get; set; }

        public string CollectorUrl { get; set; }

        public int BoundedCapacity { get; set; }

        public int ConsumerCount { get; set; }

        public int FlushInterval { get; set; }

        public string[] IgnoredRoutesRegexPatterns { get; set; }
    }
}