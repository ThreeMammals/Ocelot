namespace Ocelot.Configuration.Builder
{
    public class ServiceProviderConfigurationBuilder
    {
        private string _serviceDiscoveryProvider;
        private string _serviceDiscoveryProviderHost;
        private int _serviceDiscoveryProviderPort;

        public ServiceProviderConfigurationBuilder WithServiceDiscoveryProvider(string serviceDiscoveryProvider)
        {
            _serviceDiscoveryProvider = serviceDiscoveryProvider;
            return this;
        }

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
            return new ServiceProviderConfiguration(_serviceDiscoveryProvider, _serviceDiscoveryProviderHost,_serviceDiscoveryProviderPort);
        }
    }
}