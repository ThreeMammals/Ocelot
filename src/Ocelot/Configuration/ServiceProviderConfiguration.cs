namespace Ocelot.Configuration
{
    public class ServiceProviderConfiguration
    {
        public ServiceProviderConfiguration(string type, string host, int port, string token)
        {
            Host = host;
            Port = port;
            Token = token;
            Type = type;
        }

        public string Host { get; }
        public int Port { get; }
        public string Type { get; }
        public string Token { get; }
    }
}
