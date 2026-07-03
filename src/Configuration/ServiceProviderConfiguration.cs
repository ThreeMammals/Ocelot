namespace Ocelot.Configuration;

public class ServiceProviderConfiguration
{
    public string Scheme { get; init; }

    public string Host { get; init; }

    public int Port { get; init; }

    public string Type { get; init; }

    public string Token { get; init; }

    public string ConfigurationKey { get; init; }

    public int PollingInterval { get; init; }

    public string Namespace { get; init; }
}
