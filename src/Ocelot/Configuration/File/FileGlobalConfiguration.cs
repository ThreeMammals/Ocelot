using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileGlobalConfiguration
    {
        public FileGlobalConfiguration()
        {
            ServiceDiscoveryProvider = new FileServiceDiscoveryProvider();
            RateLimitOptions = new FileRateLimitOptions();
            LoadBalancerOptions = new FileLoadBalancerOptions();
            QoSOptions = new FileQoSOptions();
            HttpHandlerOptions = new FileHttpHandlerOptions();
            UpstreamHeaderTransform = new Dictionary<string, string>();
            DownstreamHeaderTransform = new Dictionary<string, string>();
        }

        public string RequestIdKey { get; set; }

        public FileServiceDiscoveryProvider ServiceDiscoveryProvider { get; set; }

        public FileRateLimitOptions RateLimitOptions { get; set; }

        public FileQoSOptions QoSOptions { get; set; }

        public string BaseUrl { get; set; }

        public FileLoadBalancerOptions LoadBalancerOptions { get; set; }

        public string DownstreamScheme { get; set; }

        public FileHttpHandlerOptions HttpHandlerOptions { get; set; }

        public string DownstreamHttpVersion { get; set; }
        public Dictionary<string, string> UpstreamHeaderTransform { get; set; }
        public Dictionary<string, string> DownstreamHeaderTransform { get; set; }
    }
}
