namespace Ocelot.Configuration
{
    public class LoadBalancerOptions
    {
        public LoadBalancerOptions(string type, string key, int expiryInMs)
        {
            Type = type;
            Key = key;
            ExpiryInMs = expiryInMs;
        }

        public string Type { get; }

        public string Key { get; }
        
        public int ExpiryInMs { get; } 
    }
}
