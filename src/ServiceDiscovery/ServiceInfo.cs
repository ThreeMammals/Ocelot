using Ocelot.Infrastructure.Extensions;

namespace Ocelot.ServiceDiscovery;

public readonly struct ServiceInfo
{
    public ServiceInfo(string serviceName)
    {
        Name = serviceName;
        Namespace = string.Empty;
    }
    public ServiceInfo(ReadOnlySpan<char> serviceName)
    {
        Name = serviceName.ToString();
        Namespace = string.Empty;
    }

    public ServiceInfo(string serviceName, string serviceNamespace)
    {
        Name = serviceName;
        Namespace = serviceNamespace;
    }
    public ServiceInfo(ReadOnlySpan<char> serviceName, ReadOnlySpan<char> serviceNamespace)
    {
        Name = serviceName.ToString();
        Namespace = serviceNamespace.ToString();
    }

    public ServiceInfo(ServiceInfo from)
    {
        Name = from.Name;
        Namespace = from.Namespace;
    }

    public string Name { get; init; }
    public string Namespace { get; init; }

    public const char Dot = UrlPathDynamicRouting.Dot;

    public override string ToString()
        => Namespace.IsNotEmpty()
            ? string.Concat(Namespace, Dot, Name)
            : Name;
}
