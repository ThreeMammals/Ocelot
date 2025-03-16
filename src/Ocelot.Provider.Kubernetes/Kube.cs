using KubeClient.Models;
using Ocelot.Infrastructure.DesignPatterns;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

/// <summary>Default Kubernetes service discovery provider.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>NuGet: <see href="https://www.nuget.org/packages/KubeClient">KubeClient</see></item>
/// <item>GitHub: <see href="https://github.com/tintoy/dotnet-kube-client">dotnet-kube-client</see></item>
/// </list>
/// </remarks>
public class Kube : IServiceDiscoveryProvider
{
    private static readonly (string ResourceKind, string ResourceApiVersion) EndPointsKubeKind = KubeObjectV1.GetKubeKind<EndpointsV1>();

    private readonly KubeRegistryConfiguration _configuration;
    private readonly IOcelotLogger _logger;
    private readonly IKubeApiClient _kubeApi;
    private readonly IKubeServiceBuilder _serviceBuilder;

    public Kube(
        KubeRegistryConfiguration configuration,
        IOcelotLoggerFactory factory,
        IKubeApiClient kubeApi,
        IKubeServiceBuilder serviceBuilder)
    {
        _configuration = configuration;
        _logger = factory.CreateLogger<Kube>();
        _kubeApi = kubeApi;
        _serviceBuilder = serviceBuilder;
    }

    public virtual async Task<List<Service>> GetAsync()
    {
        var endpoint = await Retry.OperationAsync(GetEndpoint, CheckErroneousState, logger: _logger);

        if (CheckErroneousState(endpoint))
        {
            _logger.LogWarning(() => GetMessage($"Unable to use bad result returned by {nameof(Kube)} integration endpoint because the final result is invalid/unknown after multiple retries!"));
            return new(0);
        }

        return BuildServices(_configuration, endpoint)
            .ToList();
    }

    private async Task<EndpointsV1> GetEndpoint()
    {
        string serviceName = _configuration.KeyOfServiceInK8s;
        string kubeNamespace = _configuration.KubeNamespace;

        try
        {
            return await _kubeApi
                .ResourceClient<IEndPointClient>(client => new EndPointClientV1(client))
                .GetAsync(serviceName, kubeNamespace);
        }
        catch (KubeApiException kubeApiError)
        {
            _logger.LogError(() =>
            {
                StatusV1 errorResponse = kubeApiError.Status;
                string httpStatusCode = "Unknown";
                if (kubeApiError.InnerException is HttpRequestException httpRequestError)
                {
                    httpStatusCode = httpRequestError.StatusCode.ToString();
                }

                return $"Failed to retrieve {EndPointsKubeKind.ResourceApiVersion}/{EndPointsKubeKind.ResourceKind} {serviceName} in namespace {kubeNamespace} ({httpStatusCode}/{errorResponse.Status}/{errorResponse.Reason}): {errorResponse.Message}";
            }, kubeApiError);
        }
        catch (HttpRequestException unexpectedRequestError)
        {
            _logger.LogError(() => $"Failed to retrieve {EndPointsKubeKind.ResourceApiVersion}/{EndPointsKubeKind.ResourceKind} {serviceName} in namespace {kubeNamespace} ({unexpectedRequestError.HttpRequestError}/{unexpectedRequestError.StatusCode}).", unexpectedRequestError);
        }
        catch (Exception unexpectedError)
        {
            _logger.LogError(() => $"Failed to retrieve {EndPointsKubeKind.ResourceApiVersion}/{EndPointsKubeKind.ResourceKind} {serviceName} in namespace {kubeNamespace} (an unexpected error occurred).", unexpectedError);
        }

        return null;
    }

    private bool CheckErroneousState(EndpointsV1 endpoint)
        => (endpoint?.Subsets?.Count ?? 0) == 0; // null or count is zero

    private string GetMessage(string message)
        => $"{nameof(Kube)} provider. Namespace:{_configuration.KubeNamespace}, Service:{_configuration.KeyOfServiceInK8s}; {message}";

    protected virtual IEnumerable<Service> BuildServices(KubeRegistryConfiguration configuration, EndpointsV1 endpoint)
        => _serviceBuilder.BuildServices(configuration, endpoint);
}
