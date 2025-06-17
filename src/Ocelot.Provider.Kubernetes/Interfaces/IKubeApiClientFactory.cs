namespace Ocelot.Provider.Kubernetes.Interfaces;

public interface IKubeApiClientFactory
{
    string ServiceAccountPath { get; set; }
    KubeApiClient Get(bool usePodServiceAccount);
}
