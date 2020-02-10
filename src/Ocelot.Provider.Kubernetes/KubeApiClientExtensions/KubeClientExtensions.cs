using KubeClient;

namespace Ocelot.Provider.Kubernetes.KubeApiClientExtensions
{
    public static class KubeClientExtensions
    {
        public static EndPointClientV1 EndPointsV1(this IKubeApiClient kubeClient)
        {
            return kubeClient.ResourceClient(client => new EndPointClientV1(client));
        }
    }
}
