namespace Ocelot.ServiceDiscovery.Configuration
{
    public interface IConsulRegistryConfiguration
    {
        string Host { get; }
        int Port { get; }
        string Token { get; }
    }

    public class ConsulRegistryConfiguration : IConsulRegistryConfiguration
    {
        public ConsulRegistryConfiguration(string host, int port, string keyOfServiceInConsul, string token)
        {
            Host = string.IsNullOrEmpty(host) ? "localhost" : host;
            Port = port > 0 ? port : 8500;
            KeyOfServiceInConsul = keyOfServiceInConsul;
            Token = token;
        }

        public string KeyOfServiceInConsul { get; }
        public string Host { get; }
        public int Port { get; }
        public string Token { get; }
    }

    public class PollingConsulRegistryConfiguration : IConsulRegistryConfiguration
    {
        public PollingConsulRegistryConfiguration(string host, int port, string keyOfServiceInConsul, string token, int delay)
        {
            Host = string.IsNullOrEmpty(host) ? "localhost" : host;
            Port = port > 0 ? port : 8500;
            KeyOfServiceInConsul = keyOfServiceInConsul;
            Token = token;
            Delay = delay;
        }

        public string KeyOfServiceInConsul { get; }
        public string Host { get; }
        public int Port { get; }
        public string Token { get; }
        public int Delay { get; }
    }
}
