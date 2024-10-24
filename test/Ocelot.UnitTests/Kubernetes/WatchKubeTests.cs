using KubeClient;
using KubeClient.Models;
using Microsoft.Reactive.Testing;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;
using System.Reactive.Linq;

namespace Ocelot.UnitTests.Kubernetes;

public class WatchKubeTests
{
    private readonly Mock<IOcelotLoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly Mock<IKubeApiClient> _kubeApiClientMock = new();
    private readonly Mock<IEndPointClient> _endpointClient = new();
    private readonly Mock<IKubeServiceBuilder> _serviceBuilderMock = new();
    private readonly TestScheduler _testScheduler = new();

    private readonly KubeRegistryConfiguration _config = new()
    {
        KubeNamespace = "dummy-namespace", KeyOfServiceInK8s = "dummy-service",
    };
    
    public WatchKubeTests()
    {
        _loggerFactoryMock
            .Setup(x => x.CreateLogger<WatchKube>())
            .Returns(_logger.Object);

        _kubeApiClientMock.Setup(x =>
                x.ResourceClient(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns(_endpointClient.Object);

        _serviceBuilderMock
            .Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns((KubeRegistryConfiguration config, EndpointsV1 endpoints) =>
            {
                return endpoints.Subsets.Select((x, i) => new Service(
                    config.KeyOfServiceInK8s,
                    new ServiceHostAndPort(x.Addresses[i].Hostname, x.Ports[i].Port!.Value),
                    i.ToString(),
                    endpoints.ApiVersion,
                    Enumerable.Empty<string>()));
            });
    }

    [Theory]
    [InlineData(ResourceEventType.Added, 1)]
    [InlineData(ResourceEventType.Modified, 1)]
    [InlineData(ResourceEventType.Bookmark, 1)]
    [InlineData(ResourceEventType.Error, 0)]
    [InlineData(ResourceEventType.Deleted, 0)]
    [Trait("Feat ", "2168")]
    public async Task GetAsync_EndpointsEventObserved_ServicesReturned(ResourceEventType eventType,
        int expectedServicesCount)
    {
        // Arrange
        var eventDelay = TimeSpan.FromMilliseconds(Random.Shared.Next(1, (WatchKube.FirstResultsFetchingTimeoutSeconds * 1000) - 1));
        var endpointsObservable = CreateOneEvent(eventType).ToObservable().Delay(eventDelay, _testScheduler);
        _endpointClient
            .Setup(x => x.Watch(
                It.Is<string>(s => s == _config.KeyOfServiceInK8s),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(endpointsObservable);

        // Act
        var watchKube = CreateWatchKube();
        _testScheduler.AdvanceBy(eventDelay.Ticks);
        var services = await watchKube.GetAsync();

        // Assert
        services.Count.ShouldBe(expectedServicesCount);
    }

    [Fact]
    [Trait("Feat ", "2168")]
    public async Task GetAsync_NoEventsAfterTimeout_EmptyServicesReturned()
    {
        // Arrange
        _endpointClient
            .Setup(x => x.Watch(
                It.Is<string>(s => s == _config.KeyOfServiceInK8s),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Observable.Create<IResourceEventV1<EndpointsV1>>(_ => Mock.Of<IDisposable>()));

        // Act
        var watchKube = CreateWatchKube();
        _testScheduler.Start();
        var services = await watchKube.GetAsync();

        // Assert
        services.ShouldBeEmpty();
        _testScheduler.Clock.ShouldBe(TimeSpan.FromSeconds(WatchKube.FirstResultsFetchingTimeoutSeconds).Ticks);
        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()));
    }

    [Fact]
    [Trait("Feat ", "2168")]
    public async Task GetAsync_WatchFailed_RetriedAfterDelay()
    {
        // Arrange
        var subscriptionAttempts = 0;
        var observable = Observable.Create<IResourceEventV1<EndpointsV1>>(observer =>
        {
            if (subscriptionAttempts == 0)
            {
                observer.OnError(new HttpRequestException("Error occured in first watch request"));
            }
            else
            {
                observer.OnNext(CreateOneEvent(ResourceEventType.Added).First());
            }

            subscriptionAttempts++;
            return Mock.Of<IDisposable>();
        });
        _endpointClient
            .Setup(x => x.Watch(
                It.Is<string>(s => s == _config.KeyOfServiceInK8s),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(observable);

        // Act
        var watchKube = CreateWatchKube();
        _testScheduler.Start();
        var services = await watchKube.GetAsync();

        // Assert
        services.Count.ShouldBe(1);
        subscriptionAttempts.ShouldBe(2);
        _testScheduler.Clock.ShouldBe(TimeSpan.FromSeconds(WatchKube.FailedSubscriptionRetrySeconds).Ticks);
        _logger.Verify(x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()));
    }

    private WatchKube CreateWatchKube() => new(_config,
        _loggerFactoryMock.Object,
        _kubeApiClientMock.Object,
        _serviceBuilderMock.Object,
        _testScheduler);

    private IResourceEventV1<EndpointsV1>[] CreateOneEvent(ResourceEventType eventType)
    {
        var resourceEvent = new ResourceEventV1<EndpointsV1>() { EventType = eventType, Resource = CreateEndpoints(), };
        return new IResourceEventV1<EndpointsV1>[] { resourceEvent };
    }

    private EndpointsV1 CreateEndpoints()
    {
        var endpoints = new EndpointsV1
        {
            Kind = "endpoint",
            ApiVersion = "1.0",
            Metadata = new ObjectMetaV1 { Name = _config.KeyOfServiceInK8s, Namespace = _config.KubeNamespace, },
        };
        var subset = new EndpointSubsetV1();
        subset.Addresses.Add(new EndpointAddressV1 { Ip = "127.0.0.1", Hostname = "localhost" });
        subset.Ports.Add(new EndpointPortV1 { Port = 80 });
        endpoints.Subsets.Add(subset);
        return endpoints;
    }
}
