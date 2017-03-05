namespace Ocelot.Configuration.Builder
{
    public class ServiceProviderConfigurationBuilder
    {
        private string _serviceName;
        private string _downstreamHost;
        private int _downstreamPort;
        private bool _userServiceDiscovery;
        private string _serviceDiscoveryProvider;
        private string _serviceDiscoveryProviderHost;
        private int _serviceDiscoveryProviderPort;

        public ServiceProviderConfigurationBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;
            return this;
        }

        public ServiceProviderConfigurationBuilder WithDownstreamHost(string downstreamHost)
        {
            _downstreamHost = downstreamHost;
            return this;
        }

        public ServiceProviderConfigurationBuilder WithDownstreamPort(int downstreamPort)
        {
            _downstreamPort = downstreamPort;
            return this;
        }

        public ServiceProviderConfigurationBuilder WithUseServiceDiscovery(bool userServiceDiscovery)
        {
            _userServiceDiscovery = userServiceDiscovery;
            return this;
        }

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
            return new ServiceProviderConfiguration(_serviceName, _downstreamHost, _downstreamPort, _userServiceDiscovery,
            _serviceDiscoveryProvider, _serviceDiscoveryProviderHost,_serviceDiscoveryProviderPort);
        }
    }
}