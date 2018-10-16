namespace Ocelot.Configuration
{
    public class ServiceProviderConfiguration
    {
        public ServiceProviderConfiguration(string type, string host, int port, string token, string configurationKey, int pollingInterval, bool useHttpsScheme = false)
        {
            ConfigurationKey = configurationKey;
            Host = host;
            Port = port;
            Token = token;
            Type = type;
            PollingInterval = pollingInterval;
            UseHttpsScheme = useHttpsScheme;
        }

        public string Host { get; }
        public int Port { get; }
        public string Type { get; }
        public string Token { get; }
        public string ConfigurationKey { get; }
        public int PollingInterval { get; }
        public bool UseHttpsScheme { get; }
    }
}
