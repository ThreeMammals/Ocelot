namespace Ocelot.Configuration.Builder
{
    public class ServiceProviderConfigurationBuilder
    {
        private string _serviceDiscoveryProviderHost;
        private int _serviceDiscoveryProviderPort;

        public ServiceProviderConfigurationBuilder WithServiceDiscoveryProviderHost(string serviceDiscoveryProviderHost)
        {
            _serviceDiscoveryProviderHost = serviceDiscoveryProviderHost;
            return this;
        }

        public ServiceProviderConfigurationBuilder WithServiceDiscoveryProviderPort(int serviceDiscoveryProviderPort)
        {
            _serviceDiscoveryProviderPort = serviceDiscoveryProviderPort;
            return this;
        }

        public ServiceProviderConfiguration Build()
        {
            return new ServiceProviderConfiguration(_serviceDiscoveryProviderHost,_serviceDiscoveryProviderPort);
        }
    }
}