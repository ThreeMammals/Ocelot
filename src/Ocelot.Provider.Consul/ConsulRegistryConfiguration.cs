namespace Ocelot.Provider.Consul;

public class ConsulRegistryConfiguration // TODO Inherit from ServiceProviderConfiguration ?
{
    /// <summary>
    /// Consul HTTP client default port.
    /// <para>
    /// HashiCorp Developer docs: <see href="https://developer.hashicorp.com/consul">Consul</see> | <see href="https://developer.hashicorp.com/consul/docs/install/ports">Required Ports</see>.
    /// </para>
    /// </summary>
    public const int DefaultHttpPort = 8500;

    public ConsulRegistryConfiguration(string scheme, string host, int port, string keyOfServiceInConsul, string token)
    {
        // TODO Why not to encapsulate this biz logic right in ConsulProviderFactory? LoL
        Host = string.IsNullOrEmpty(host) ? "localhost" : host;
        Port = port > 0 ? port : DefaultHttpPort;
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
