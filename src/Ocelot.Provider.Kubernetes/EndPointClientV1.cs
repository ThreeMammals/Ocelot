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

    public Task<EndpointsV1> GetAsync(string serviceName, string kubeNamespace = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentNullException(nameof(serviceName));
        }

        var request = EndpointsRequest
            .WithTemplateParameters(new
            {
                Namespace = kubeNamespace ?? KubeClient.DefaultNamespace, ServiceName = serviceName,
            });

        return Http.GetAsync(request, cancellationToken)
            .ReadContentAsObjectV1Async<EndpointsV1>(operationDescription: $"get {nameof(EndpointsV1)}");
    }

    public IObservable<IResourceEventV1<EndpointsV1>> Watch(string serviceName, string kubeNamespace,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentNullException(nameof(serviceName));
        }

        return ObserveEvents<EndpointsV1>(
            EndpointsWatchRequest.WithTemplateParameters(new
            {
                ServiceName = serviceName,
                Namespace = kubeNamespace ?? KubeClient.DefaultNamespace,
            }),
            "watch v1/Endpoints '" + serviceName + "' in namespace " +
            (kubeNamespace ?? KubeClient.DefaultNamespace));
    }
}
