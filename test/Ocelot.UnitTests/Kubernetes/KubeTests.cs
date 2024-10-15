using KubeClient;
using KubeClient.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Testing;
using Ocelot.Values;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Ocelot.UnitTests.Kubernetes;

public class KubeTests
{
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
            endpoints,
            out Lazy<string> receivedToken);

        // Act
        var services = await given.Provider.GetAsync();

        // Assert
        services.ShouldNotBeNull().Count.ShouldBe(1);
        receivedToken.Value.ShouldBe($"Bearer {nameof(Should_return_service_from_k8s)}");
    }

    [Fact]
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

    private EndpointsV1 GivenEndpoints(
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

    private IWebHost GivenThereIsAFakeKubeServiceDiscoveryProvider(string url, string namespaces, string serviceName,
        EndpointsV1 endpointEntries, out Lazy<string> receivedToken)
    {
        var token = string.Empty;
        receivedToken = new(() => token);

        Task ProcessKubernetesRequest(HttpContext context)
        {
            if (context.Request.Path.Value == $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
            {
                if (context.Request.Headers.TryGetValue("Authorization", out var values))
                {
                    token = values.First();
                }

                var json = JsonSerializer.Serialize(endpointEntries, JsonSerializerOptionsFactory.Web);
                context.Response.Headers.Append("Content-Type", "application/json");
                return context.Response.WriteAsync(json);
            }

            return Task.CompletedTask;
        }

        var host = new WebHostBuilder()
            .UseUrls(url)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(url)
            .Configure(app => app.Run(ProcessKubernetesRequest))
            .Build();
        host.Start();
        return host;
    }
}
