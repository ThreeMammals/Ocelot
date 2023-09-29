namespace Ocelot.Provider.Consul;

public class ConsulRegistryConfiguration
{
    public ConsulRegistryConfiguration(string scheme, string host, int port, string keyOfServiceInConsul, string token)
    {
        Host = string.IsNullOrEmpty(host) ? "localhost" : host;
        Port = port > 0 ? port : 8500; // Is 8500 default port of Consul?
        Scheme = string.IsNullOrEmpty(scheme) ? Uri.UriSchemeHttp : scheme;
        KeyOfServiceInConsul = keyOfServiceInConsul;
        Token = token;
    }

    public string KeyOfServiceInConsul { get; }
    public string Scheme { get; }
    public string Host { get; }
    public int Port { get; }
    public string Token { get; }
}
