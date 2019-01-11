namespace Ocelot.Provider.Consul
{
    public class ConsulRegistryConfiguration
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
}
