namespace Ocelot.Configuration.Builder
{
    public class ServiceProviderConfiguraionBuilder
    {
        private string _serviceName;
        private string _downstreamHost;
        private int _downstreamPort;
        private bool _userServiceDiscovery;
        private string _serviceDiscoveryProvider;
        private string _serviceDiscoveryProviderHost;
        private int _serviceDiscoveryProviderPort;

        public ServiceProviderConfiguraionBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;
            return this;
        }

        public ServiceProviderConfiguraionBuilder WithDownstreamHost(string downstreamHost)
        {
            _downstreamHost = downstreamHost;
            return this;
        }

        public ServiceProviderConfiguraionBuilder WithDownstreamPort(int downstreamPort)
        {
            _downstreamPort = downstreamPort;
            return this;
        }

        public ServiceProviderConfiguraionBuilder WithUseServiceDiscovery(bool userServiceDiscovery)
        {
            _userServiceDiscovery = userServiceDiscovery;
            return this;
        }

        public ServiceProviderConfiguraionBuilder WithServiceDiscoveryProvider(string serviceDiscoveryProvider)
        {
            _serviceDiscoveryProvider = serviceDiscoveryProvider;
            return this;
        }

        public ServiceProviderConfiguraionBuilder WithServiceDiscoveryProviderHost(string serviceDiscoveryProviderHost)
        {
            _serviceDiscoveryProviderHost = serviceDiscoveryProviderHost;
            return this;
        }

        public ServiceProviderConfiguraionBuilder WithServiceDiscoveryProviderPort(int serviceDiscoveryProviderPort)
        {
            _serviceDiscoveryProviderPort = serviceDiscoveryProviderPort;
            return this;
        }

        
        public ServiceProviderConfiguraion Build()
        {
            return new ServiceProviderConfiguraion(_serviceName, _downstreamHost, _downstreamPort, _userServiceDiscovery,
            _serviceDiscoveryProvider, _serviceDiscoveryProviderHost,_serviceDiscoveryProviderPort);
        }
    }
}