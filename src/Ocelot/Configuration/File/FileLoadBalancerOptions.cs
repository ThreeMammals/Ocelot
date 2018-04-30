namespace Ocelot.Configuration.File
{
    public class FileLoadBalancerOptions
    {
        public string Type { get; set; }
        public string Key { get; set; }
        public int ExpiryInMs { get; set; } 
    }
}
