namespace Ocelot.Configuration.Builder
{
    public class ServiceProviderConfigurationBuilder
    {
        private string _serviceDiscoveryProviderHost;
        private int _serviceDiscoveryProviderPort;
        private string _type;
        private string _token;

        public ServiceProviderConfigurationBuilder WithHost(string serviceDiscoveryProviderHost)
        {
            _serviceDiscoveryProviderHost = serviceDiscoveryProviderHost;
            return this;
        }

        public ServiceProviderConfigurationBuilder WithPort(int serviceDiscoveryProviderPort)
        {
            _serviceDiscoveryProviderPort = serviceDiscoveryProviderPort;
            return this;
        }

        public ServiceProviderConfigurationBuilder WithType(string type)
        {
            _type = type;
            return this;
        }

        public ServiceProviderConfigurationBuilder WithToken(string token)
        {
            _token = token;
            return this;
        }

        public ServiceProviderConfiguration Build()
        {
            return new ServiceProviderConfiguration(_type, _serviceDiscoveryProviderHost, _serviceDiscoveryProviderPort, _token);
        }
    }
}
