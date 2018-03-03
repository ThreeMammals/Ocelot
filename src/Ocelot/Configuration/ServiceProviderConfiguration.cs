namespace Ocelot.Configuration
{
    public class ServiceProviderConfiguration
    {
        public ServiceProviderConfiguration(string type, string host, int port)
        {
            Host = host;
            Port = port;
            Type = type;
        }

        public string Host { get; private set; }
        public int Port { get; private set; }
        public string Type { get; private set; }
    }
}