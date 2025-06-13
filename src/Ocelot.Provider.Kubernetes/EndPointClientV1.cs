using KubeClient.Http;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes;

public class EndPointClientV1 : KubeResourceClient, IEndPointClient
{
    private static readonly HttpRequest EndpointsRequest =
        KubeRequest.Create("api/v1/namespaces/{Namespace}/endpoints/{ServiceName}");

    private static readonly HttpRequest EndpointsWatchRequest =
        KubeRequest.Create("api/v1/watch/namespaces/{Namespace}/endpoints/{ServiceName}");

    public EndPointClientV1(IKubeApiClient client) : base(client)
    {
    }

    public Task<EndpointsV1> GetAsync(string serviceName, string kubeNamespace = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(serviceName);

        var request = EndpointsRequest.WithTemplateParameters(new
        {
            Namespace = kubeNamespace ?? KubeClient.DefaultNamespace,
            ServiceName = serviceName,
        });

        return Http.GetAsync(request, cancellationToken)
            .ReadContentAsObjectV1Async<EndpointsV1>(operationDescription: $"{nameof(GetAsync)} {nameof(EndpointsV1)}");
    }

    public IObservable<IResourceEventV1<EndpointsV1>> Watch(string serviceName, string kubeNamespace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(serviceName);

        var request = EndpointsWatchRequest.WithTemplateParameters(new
        {
            ServiceName = serviceName,
            Namespace = kubeNamespace ?? KubeClient.DefaultNamespace,
        });

        return ObserveEvents<EndpointsV1>(request,
            $"{nameof(Watch)} {nameof(EndpointsV1)} for '{serviceName}' in the namespace '{kubeNamespace ?? KubeClient.DefaultNamespace}'");
    }
}
