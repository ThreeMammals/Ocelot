using KubeClient;
using KubeClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Ocelot.UnitTests.Kubernetes;

[Trait("Feat", "2168")]
[Trait("PR", "2174")] // https://github.com/ThreeMammals/Ocelot/pull/2174
public class WatchKubeTests
{
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory = new();
    private readonly Mock<IKubeApiClient> _kubeApiClient = new();
    private readonly Mock<IEndPointClient> _endpointClient = new();
    private readonly Mock<IKubeServiceBuilder> _kubeServiceBuilder = new();
    private readonly TestScheduler _testScheduler = new();
    private readonly KubeRegistryConfiguration _config = new()
    {
        KubeNamespace = "dummy-namespace", KeyOfServiceInK8s = "dummy-service",
    };
    private readonly OcelotLogger _ocLogger;
    private readonly Mock<ILogger> _logger = new();
    private readonly Mock<IRequestScopedDataRepository> _dataRepository = new();
    private readonly Expression<Func<IEndPointClient, IObservable<IResourceEventV1<EndpointsV1>>>> _watch;

    public WatchKubeTests()
    {
        _logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);
        _logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<Func<string, Exception, string>>()))
            .Verifiable();
        _dataRepository.Setup(x => x.Get<string>(It.IsAny<string>()))
            .Returns((Response<string>)null);
        _ocLogger = new(_logger.Object, _dataRepository.Object);
        _loggerFactory.Setup(x => x.CreateLogger<WatchKube>())
            .Returns(_ocLogger);
        _kubeApiClient.Setup(x => x.ResourceClient(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns(_endpointClient.Object);
        _kubeServiceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns((KubeRegistryConfiguration config, EndpointsV1 endpoints) =>
            {
                return endpoints.Subsets.Select((x, i) => new Service(
                    config.KeyOfServiceInK8s,
                    new ServiceHostAndPort(x.Addresses[i].Hostname, x.Ports[i].Port!.Value),
                    i.ToString(),
                    endpoints.ApiVersion,
                    Enumerable.Empty<string>()));
            });
        _watch = x => x.Watch(It.Is<string>(s => s == _config.KeyOfServiceInK8s), It.IsAny<string>(), It.IsAny<CancellationToken>());
    }

    [Theory]
    [InlineData(ResourceEventType.Added, 1)]
    [InlineData(ResourceEventType.Modified, 1)]
    [InlineData(ResourceEventType.Bookmark, 1)]
    [InlineData(ResourceEventType.Error, 0)]
    [InlineData(ResourceEventType.Deleted, 0)]
    public async Task GetAsync_EndpointsEventObserved_ServicesReturned(ResourceEventType eventType, int expectedServicesCount)
    {
        // Arrange
        var eventDelay = TimeSpan.FromMilliseconds(Random.Shared.Next(1, (WatchKube.FirstResultsFetchingTimeoutSeconds * 1000) - 1));
        var endpointsObservable = CreateOneEvent(eventType).ToObservable().Delay(eventDelay, _testScheduler);
        _endpointClient.Setup(_watch).Returns(endpointsObservable);

        // Act
        var watchKube = CreateWatchKube();
        _testScheduler.AdvanceBy(eventDelay.Ticks);
        var services = await watchKube.GetAsync();

        // Assert
        services.Count.ShouldBe(expectedServicesCount);
    }

    [Fact]
    public async Task GetAsync_NoEventsAfterTimeout_EmptyServicesReturned()
    {
        // Arrange
        _endpointClient.Setup(_watch)
            .Returns(Observable.Create<IResourceEventV1<EndpointsV1>>(_ => Mock.Of<IDisposable>()));

        // Act
        var watchKube = CreateWatchKube();
        _testScheduler.Start();
        var services = await watchKube.GetAsync();

        // Assert
        services.ShouldBeEmpty();
        _testScheduler.Clock.ShouldBe(TimeSpan.FromSeconds(WatchKube.FirstResultsFetchingTimeoutSeconds).Ticks);
        _logger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<Func<string, Exception, string>>()),
            Times.Once());
    }

    [Fact]
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
        _endpointClient.Setup(_watch).Returns(observable);

        // Act
        var watchKube = CreateWatchKube();
        _testScheduler.Start();
        var services = await watchKube.GetAsync();

        // Assert
        services.Count.ShouldBe(1);
        subscriptionAttempts.ShouldBe(2);
        _testScheduler.Clock.ShouldBe(TimeSpan.FromSeconds(WatchKube.FailedSubscriptionRetrySeconds).Ticks);
        _logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<Func<string, Exception, string>>()),
            Times.Once());
    }

    [Fact]
    public async Task Dispose_OnSubscriptionCancellation_LogsInformation()
    {
        // Arrange
        var observable = Observable.Create<IResourceEventV1<EndpointsV1>>(observer =>
        {
            observer.OnCompleted();
            return Mock.Of<IDisposable>();
        });
        _endpointClient.Setup(_watch).Returns(observable);

        // Act
        var watchKube = CreateWatchKube();
        _testScheduler.Start();
        var services = await watchKube.GetAsync();
        watchKube.Dispose();

        // Assert
        services.ShouldBeEmpty();
        _testScheduler.Clock.ShouldBe(TimeSpan.FromSeconds(WatchKube.FirstResultsFetchingTimeoutSeconds).Ticks);
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<Func<string, Exception, string>>()),
            Times.Once());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAsync_EndpointsEventObserved_NoServices(bool branch1)
    {
        // Arrange
        var eventDelay = TimeSpan.FromMilliseconds(Random.Shared.Next(1, (WatchKube.FirstResultsFetchingTimeoutSeconds * 1000) - 1));
        EndpointsV1 endpoints = null;
        if (branch1)
        {
            endpoints = new EndpointsV1
            {
                Kind = "endpoint", ApiVersion = "1.0",
                Metadata = new ObjectMetaV1 { Name = _config.KeyOfServiceInK8s, Namespace = _config.KubeNamespace, },
            };
            endpoints.Subsets.Clear();
        }

        var resourceEvent = new ResourceEventV1<EndpointsV1> { EventType = ResourceEventType.Bookmark, Resource = endpoints, };
        var events = new ResourceEventV1<EndpointsV1>[] { resourceEvent };
        var endpointsObservable = events.ToObservable().Delay(eventDelay, _testScheduler);
        _endpointClient.Setup(_watch).Returns(endpointsObservable);

        // Act
        var watchKube = CreateWatchKube();
        _testScheduler.AdvanceBy(eventDelay.Ticks);
        var services = await watchKube.GetAsync();

        // Assert
        services.ShouldBeEmpty();
    }

    private WatchKube CreateWatchKube() => new(_config, _loggerFactory.Object, _kubeApiClient.Object, _kubeServiceBuilder.Object, _testScheduler);

    private IResourceEventV1<EndpointsV1>[] CreateOneEvent(ResourceEventType eventType)
    {
        var resourceEvent = new ResourceEventV1<EndpointsV1> { EventType = eventType, Resource = CreateEndpoints(), };
        return [resourceEvent];
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
