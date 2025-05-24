using KubeClient.Http;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes;

public class EndPointClientV1 : KubeResourceClient, IEndPointClient
{
    private static readonly HttpRequest Collection = KubeRequest.Create("api/v1/namespaces/{Namespace}/endpoints/{ServiceName}");

    public EndPointClientV1(IKubeApiClient client) : base(client)
    {
    }

    public Task<EndpointsV1> GetAsync(string serviceName, string kubeNamespace = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentNullException(nameof(serviceName));
        }

        var request = Collection
            .WithTemplateParameters(new
            {
                Namespace = kubeNamespace ?? KubeClient.DefaultNamespace,
                ServiceName = serviceName,
            });

        return Http.GetAsync(request, cancellationToken)
            .ReadContentAsObjectV1Async<EndpointsV1>(operationDescription: $"get {nameof(EndpointsV1)}");
    }
}
