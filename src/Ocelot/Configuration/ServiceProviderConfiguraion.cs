namespace Ocelot.Configuration
{
    public class ServiceProviderConfiguration
    {
        public ServiceProviderConfiguration(string serviceProviderHost, int serviceProviderPort)
        {
            ServiceProviderHost = serviceProviderHost;
            ServiceProviderPort = serviceProviderPort;
        }

        public string ServiceProviderHost { get; private set; }
        public int ServiceProviderPort { get; private set; }
    }
}