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

    private string Message(string details)
        => $"Failed to retrieve {EndPointsKubeKind.ResourceApiVersion}/{EndPointsKubeKind.ResourceKind} '{_configuration.KeyOfServiceInK8s}' in namespace '{_configuration.KubeNamespace}': {details}";

    private async Task<EndpointsV1> GetEndpoint()
    {
        try
        {
            return await _kubeApi
                .EndpointsV1()
                .GetAsync(_configuration.KeyOfServiceInK8s, _configuration.KubeNamespace);
        }
        catch (KubeApiException ex)
        {
            string Msg()
            {
                StatusV1 status = ex.Status;
                string httpStatusCode = "-"; // Unknown
                if (ex.InnerException is HttpRequestException e)
                {
                    httpStatusCode = e.StatusCode.ToString();
                }

                return Message($"(HTTP.{httpStatusCode}/{status.Status}/{status.Reason}): {status.Message}");
            }

            _logger.LogError(Msg, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(() => Message($"({ex.HttpRequestError}/HTTP.{ex.StatusCode})."), ex);
        }
        catch (Exception unexpected)
        {
            _logger.LogError(() => Message($"(an unexpected ex occurred)."), unexpected);
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
