namespace Ocelot.ServiceDiscovery.Providers
{
    using Pivotal.Discovery.Client;

    public class EurekaServiceDiscoveryFactory : IEurekaServiceDiscoveryFactory
    {
        private readonly IDiscoveryClient _discoveryClient;

        public EurekaServiceDiscoveryFactory(IDiscoveryClient discoveryClient)
        {
            _discoveryClient = discoveryClient;
        }

        public IDiscoveryClient Get()
        {
            return _discoveryClient;
        }
    }
}
