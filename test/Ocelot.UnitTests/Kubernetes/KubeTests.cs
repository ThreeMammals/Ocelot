using KubeClient;
using KubeClient.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.Kubernetes;

/// <summary>
/// Contains integration tests.
/// Move to integration testing, and add at least one "happy path" unit test.
/// </summary>
[Collection(nameof(SequentialTests))]
public class KubeTests : FileUnitTest
{
    static JsonSerializerSettings JsonSerializerSettings => KubeClient.ResourceClients.KubeResourceClient.SerializerSettings;

    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;

    public KubeTests()
    {
        _factory = new();
        _logger = new();
        _factory.Setup(x => x.CreateLogger<Kube>()).Returns(_logger.Object);
    }

    [Fact]
    [Trait("Feat", "345")]
    public async Task Should_return_service_from_k8s()
    {
        // Arrange
        var given = GivenClientAndProvider(out var serviceBuilder);
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new Service[] { new(nameof(Should_return_service_from_k8s), new("localhost", 80), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpoints();
        using var kubernetes = GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            responseStatusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> receivedToken);

        // Act
        var services = await given.Provider.GetAsync();

        // Assert
        services.ShouldNotBeNull().Count.ShouldBe(1);
        receivedToken.Value.ShouldBe($"Bearer {nameof(Should_return_service_from_k8s)}");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    [Trait("PR", "2266")]
    public async Task Should_not_return_service_from_k8s_when_k8s_api_returns_error_response(HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var given = GivenClientAndProvider(out var serviceBuilder);
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new Service[] { new(nameof(Should_not_return_service_from_k8s_when_k8s_api_returns_error_response), new("localhost", 80), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpoints();
        using var kubernetes = GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            expectedStatusCode,
            endpoints,
            out Lazy<string> receivedToken);

        string expectedKubeApiErrorMessage = GetKubeApiErrorMessage(serviceName: given.ProviderOptions.KeyOfServiceInK8s, given.ProviderOptions.KubeNamespace, expectedStatusCode);
        string expectedLogMessage = $"Failed to retrieve v1/Endpoints '{given.ProviderOptions.KeyOfServiceInK8s}' in namespace '{given.ProviderOptions.KubeNamespace}': (HTTP.{expectedStatusCode}/Failure/{expectedStatusCode}): {expectedKubeApiErrorMessage}";
        _logger.Setup(logger => logger.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()))
            .Callback((Func<string> messageFactory, Exception exception) =>
            {
                messageFactory.ShouldNotBeNull();

                string logMessage = messageFactory();
                logMessage.ShouldNotBeNullOrWhiteSpace();

                // This is a little fragile, as it may change if other entries are logged due to implementation changes.
                // Unfortunately, the use of a factory delegate for the log message, combined with reuse of Kube's logger for Retry.OperationAsync makes this tricky to test any other way so this is probably the best we can do for now.
                if (logMessage.StartsWith("Ocelot Retry strategy"))
                {
                    return;
                }

                logMessage.ShouldBe(expectedLogMessage);

                exception.ShouldNotBeNull();
                KubeApiException kubeApiException = exception.ShouldBeOfType<KubeApiException>();
                StatusV1 errorResponse = kubeApiException.Status;
                errorResponse.Status.ShouldBe(StatusV1.FailureStatus);
                errorResponse.Code.ShouldBe((int)expectedStatusCode);
                errorResponse.Reason.ShouldBe(expectedStatusCode.ToString());
                errorResponse.Message.ShouldNotBeNullOrWhiteSpace();
            })
            .Verifiable($"IOcelotLogger.LogError() was not called.");

        // Act
        var services = await given.Provider.GetAsync();

        // Assert
        services.ShouldNotBeNull().Count.ShouldBe(0);
        receivedToken.Value.ShouldBe($"Bearer {nameof(Should_not_return_service_from_k8s_when_k8s_api_returns_error_response)}");
        _logger.Verify();
    }

    [Fact] // This is not unit test! LoL :) This should be an integration test or even an acceptance test...
    [Trait("Bug", "2110")]
    public async Task Should_return_single_service_from_k8s_during_concurrent_calls()
    {
        // Arrange
        var given = GivenClientAndProvider(out var serviceBuilder);
        var manualResetEvent = new ManualResetEvent(false);
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() =>
            {
                manualResetEvent.WaitOne();
                return new Service[] { new(nameof(Should_return_single_service_from_k8s_during_concurrent_calls), new("localhost", 80), string.Empty, string.Empty, Array.Empty<string>()) };
            });

        var endpoints = GivenEndpoints();
        using var kubernetes = GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            endpoints,
            out Lazy<string> receivedToken);

        // Act
        var services = new List<Service>();
        async Task WhenIGetTheServices() => services = await given.Provider.GetAsync();
        var getServiceTasks = Task.WhenAll(
            WhenIGetTheServices(),
            WhenIGetTheServices());
        manualResetEvent.Set();
        await getServiceTasks;

        // Assert
        receivedToken.Value.ShouldBe($"Bearer {nameof(Should_return_single_service_from_k8s_during_concurrent_calls)}");
        services.ShouldNotBeNull().Count.ShouldBe(1);
        services.ShouldAllBe(s => s != null);
    }

    private (IKubeApiClient Client, KubeClientOptions ClientOptions, Kube Provider, KubeRegistryConfiguration ProviderOptions)
        GivenClientAndProvider(out Mock<IKubeServiceBuilder> serviceBuilder, string namespaces = null, [CallerMemberName] string serviceName = null)
    {
        namespaces ??= nameof(KubeTests);
        var kubePort = PortFinder.GetRandomPort();
        serviceName ??= "test" + kubePort;
        var kubeEndpointUrl = $"{Uri.UriSchemeHttp}://localhost:{kubePort}";
        var options = new KubeClientOptions
        {
            ApiEndPoint = new Uri(kubeEndpointUrl),
            AccessToken = serviceName, // "txpc696iUhbVoudg164r93CxDTrKRVWG",
            AuthStrategy = KubeAuthStrategy.BearerToken,
            AllowInsecure = true,
        };
        IKubeApiClient client = KubeApiClient.Create(options);

        var config = new KubeRegistryConfiguration
        {
            KeyOfServiceInK8s = serviceName,
            KubeNamespace = namespaces,
        };
        serviceBuilder = new();
        var provider = new Kube(config, _factory.Object, client, serviceBuilder.Object);
        return (client, options, provider, config);
    }

    protected EndpointsV1 GivenEndpoints(
        string namespaces = nameof(KubeTests),
        [CallerMemberName] string serviceName = "test")
    {
        var endpoints = new EndpointsV1
        {
            Kind = "endpoint",
            ApiVersion = "1.0",
            Metadata = new ObjectMetaV1
            {
                Name = serviceName,
                Namespace = namespaces,
            },
        };
        var subset = new EndpointSubsetV1();
        subset.Addresses.Add(new EndpointAddressV1
        {
            Ip = "127.0.0.1",
            Hostname = "localhost",
        });
        subset.Ports.Add(new EndpointPortV1
        {
            Port = 80,
        });
        endpoints.Subsets.Add(subset);
        return endpoints;
    }

    protected IWebHost GivenThereIsAFakeKubeServiceDiscoveryProvider(string url, string namespaces, string serviceName,
        EndpointsV1 endpointEntries, out Lazy<string> receivedToken)
    {
        return GivenThereIsAFakeKubeServiceDiscoveryProvider(url, namespaces, serviceName, HttpStatusCode.OK, endpointEntries, out receivedToken);
    }

    protected IWebHost GivenThereIsAFakeKubeServiceDiscoveryProvider(string url, string namespaces, string serviceName,
        HttpStatusCode responseStatusCode, EndpointsV1 endpointEntries, out Lazy<string> receivedToken)
    {
        var token = string.Empty;
        receivedToken = new(() => token);

        Task ProcessKubernetesRequest(HttpContext context)
        {
            if (context.Request.Path.Value == $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
            {
                string responseBody;

                if (context.Request.Headers.TryGetValue("Authorization", out var values))
                {
                    token = values.First();
                }

                if (responseStatusCode == HttpStatusCode.OK)
                {
                    responseBody = JsonConvert.SerializeObject(endpointEntries, JsonSerializerSettings);
                }
                else
                {
                    responseBody = JsonConvert.SerializeObject(new StatusV1
                    {
                        Message = GetKubeApiErrorMessage(serviceName, namespaces, responseStatusCode),
                        Reason = responseStatusCode.ToString(),
                        Code = (int)responseStatusCode,
                        Status = StatusV1.FailureStatus,
                    }, JsonSerializerSettings);
                }

                context.Response.StatusCode = (int)responseStatusCode;
                context.Response.Headers.Append("Content-Type", "application/json");
                return context.Response.WriteAsync(responseBody);
            }

            return Task.CompletedTask;
        }

        var host = TestHostBuilder.Create()
            .UseUrls(url)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(url)
            .Configure(app => app.Run(ProcessKubernetesRequest))
            .Build();
        host.Start(); // problematic starting in case of parallel running of unit tests because of failing of port binding
        return host;
    }

    private static string GetKubeApiErrorMessage(string serviceName, string kubeNamespace, HttpStatusCode responseStatusCode)
    {
        return $"Failed to retrieve v1/Endpoints '{serviceName}' in namespace '{kubeNamespace}' (HTTP.{responseStatusCode}/Failure/{responseStatusCode}): This is an error response for HTTP status code {(int)responseStatusCode} ('{responseStatusCode}') from the fake Kubernetes API.";
    }
}
