namespace Ocelot.Configuration.Builder
{
    public class ServiceProviderConfigurationBuilder
    {
        private string _serviceDiscoveryProviderHost;
        private int _serviceDiscoveryProviderPort;
        private string _type;
        private string _token;
        private string _configurationKey;
        private int _pollingInterval;

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

        public ServiceProviderConfigurationBuilder WithConfigurationKey(string configurationKey)
        {
            _configurationKey = configurationKey;
            return this;
        }

        public ServiceProviderConfigurationBuilder WithPollingInterval(int pollingInterval)
        {
            _pollingInterval = pollingInterval;
            return this;
        }

        public ServiceProviderConfiguration Build()
        {
            return new ServiceProviderConfiguration(_type, _serviceDiscoveryProviderHost, _serviceDiscoveryProviderPort, _token, _configurationKey, _pollingInterval);
        }
    }
}
