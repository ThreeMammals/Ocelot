using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.Configuration
{
    public class LoadBalancerOptions
    {
        public LoadBalancerOptions(string type, string key, int expiryInMs)
        {
            Type = type ?? nameof(NoLoadBalancer);
            Key = key;
            ExpiryInMs = expiryInMs;
        }

        public string Type { get; }

        public string Key { get; }
        
        public int ExpiryInMs { get; } 
    }
}
