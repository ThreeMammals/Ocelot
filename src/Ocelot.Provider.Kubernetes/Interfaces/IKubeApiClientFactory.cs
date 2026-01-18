namespace Ocelot.Provider.Kubernetes.Interfaces;

public interface IKubeApiClientFactory
{
    KubeApiClient Get(bool usePodServiceAccount);
}
