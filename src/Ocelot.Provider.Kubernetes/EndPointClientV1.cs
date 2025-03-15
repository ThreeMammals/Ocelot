using KubeClient.Http;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes;

public class EndPointClientV1 : KubeResourceClient, IEndPointClient
{
    private static readonly HttpRequest Collection = KubeRequest.Create("api/v1/namespaces/{Namespace}/endpoints/{ServiceName}");

    private readonly ILogger _logger;

    public EndPointClientV1(IKubeApiClient client) : base(client)
    {
        _logger = client.LoggerFactory.CreateLogger<EndPointClientV1>();
    }

    public async Task<EndpointsV1> GetAsync(string serviceName, string kubeNamespace = null, CancellationToken cancellationToken = default)
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

        var response = await Http.GetAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StatusV1 errorResponse = await response.ReadContentAsAsync<StatusV1>();

                (string resourceKind, string resourceApiVersion) = KubeObjectV1.GetKubeKind<EndpointsV1>();
                _logger.LogDebug("Failed to retrieve {ResourceApiVersion}/{ResourceKind} {ResourceName} in namespace {ResourceNamespace} ({HttpStatusCode}/{Status}/{StatusReason}): {StatusMessage}",
                    resourceApiVersion,
                    resourceKind,
                    serviceName,
                    kubeNamespace,
                    response.StatusCode,
                    errorResponse.Status,
                    errorResponse.Reason,
                    errorResponse.Message
                );
            }

            return null;
        }

        return await response.ReadContentAsAsync<EndpointsV1>();
    }
}
