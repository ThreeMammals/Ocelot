using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes;

public static class KubeApiClientExtensions
{
    public static IEndPointClient EndpointsV1(this IKubeApiClient client)
        => client.ResourceClient(x => new EndPointClientV1(client));
}
