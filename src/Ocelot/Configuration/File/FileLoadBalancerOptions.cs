namespace Ocelot.Configuration.File
{
    public class FileLoadBalancerOptions
    {
        public FileLoadBalancerOptions()
        {
            Expiry = int.MaxValue;
            Key = string.Empty;
            Type = string.Empty;
        }

        public FileLoadBalancerOptions(FileLoadBalancerOptions from)
        {
            Expiry = from.Expiry;
            Key = from.Key;
            Type = from.Type;
        }

        public int Expiry { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }
    }
}
