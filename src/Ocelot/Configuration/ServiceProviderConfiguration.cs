namespace Ocelot.Configuration
{
    public class ServiceProviderConfiguration
    {
        public ServiceProviderConfiguration(string type, string host, int port, string token, string configurationKey, int pollingInterval)
        {
            ConfigurationKey = configurationKey;
            Host = host;
            Port = port;
            Token = token;
            Type = type;
            PollingInterval = pollingInterval;
        }

        public string Host { get; }
        public int Port { get; }
        public string Type { get; }
        public string Token { get; }
        public string ConfigurationKey { get; }
        public int PollingInterval { get; }
    }
}
