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
        KubeNamespace = "dummy-namespace",
        KeyOfServiceInK8s = "dummy-service",
    };

    private WatchKube _watchKube;
    private List<Service> _resultServices; 

    public WatchKubeTests()
    {
        _loggerFactoryMock
            .Setup(x => x.CreateLogger<WatchKube>())
            .Returns(_logger.Object);
        _kubeApiClientMock.Setup(x =>
                x.ResourceClient(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns(_endpointClient.Object);
    }

    [Theory]
    [InlineData(ResourceEventType.Added, 1)]
    [InlineData(ResourceEventType.Modified, 1)]
    [InlineData(ResourceEventType.Bookmark, 1)]
    [InlineData(ResourceEventType.Error, 0)]
    [InlineData(ResourceEventType.Deleted, 0)]
    [Trait("Feat ", "2168")]
    public void GetAsync_EventObserved_ServicesReturned(ResourceEventType eventType, int expectedServicesCount)
    {
        this.Given(s => s.GivenEndpointsEventObservedAfter(eventType, TimeSpan.Zero))
            .When(s => s.WhenServiceBuilderMaps())
            .When(s => s.WhenWatchStarted())
            .When(s => s.WhenTimePassing())
            .When(s => s.WhenServicesRequested())
            .Then(s => s.ThenServicesCountIs(expectedServicesCount))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat ", "2168")]
    public void GetAsync_NoEventsAfterTimeout_EmptyServicesReturned()
    {
        this.Given(s => s.GivenEndpointsEventNeverObserved())
            .When(s => s.WhenServiceBuilderMaps())
            .When(s => s.WhenWatchStarted())
            .When(s => s.WhenTimePassing())
            .When(s => s.WhenServicesRequested())
            .Then(s => s.ThenServicesCountIs(0))
            .And(s => s.AndThenTimePassed(TimeSpan.FromSeconds(WatchKube.FirstResultsFetchingTimeoutSeconds)))
            .And(s => s.AndThenWarningMessageLogged())
            .BDDfy();
    }
    
    [Fact]
    [Trait("Feat ", "2168")]
    public void GetAsync_WatchFailed_RetriedAfterDelay()
    {
        this.Given(s => s.GivenFirstSubscriptionFailed())
            .When(s => s.WhenServiceBuilderMaps())
            .When(s => s.WhenWatchStarted())
            .When(s => s.WhenTimePassing())
            .When(s => s.WhenServicesRequested())
            .Then(s => s.ThenServicesCountIs(1))
            .And(s => s.AndThenTimePassed(TimeSpan.FromSeconds(WatchKube.FailedSubscriptionRetrySeconds)))
            .And(s => s.AndThenErrorMessageLogged())
            .BDDfy();
    }
    
    private void GivenEndpointsEventObservedAfter(ResourceEventType eventType, TimeSpan timeSpan)
    {
        _endpointClient
            .Setup(x => x.Watch(
                It.Is<string>(s => s == _config.KeyOfServiceInK8s),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateOneEvent(eventType).ToObservable().Delay(timeSpan, _testScheduler));
    }
    
    private void GivenEndpointsEventNeverObserved()
    {
        _endpointClient
            .Setup(x => x.Watch(
                It.Is<string>(s => s == _config.KeyOfServiceInK8s),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Observable.Create<IResourceEventV1<EndpointsV1>>(_ => Mock.Of<IDisposable>()));
    }
    
    private void GivenFirstSubscriptionFailed()
    {
        var shouldFail = true;
        var observable = Observable.Create<IResourceEventV1<EndpointsV1>>(observer =>
        {
            if (shouldFail)
            {
                observer.OnError(new HttpRequestException("Error occured in warch request"));
                shouldFail = false;
            }
            else
            {
                observer.OnNext(CreateOneEvent(ResourceEventType.Added).First());
            }

            return Mock.Of<IDisposable>();
        });
        _endpointClient
            .Setup(x => x.Watch(
                It.Is<string>(s => s == _config.KeyOfServiceInK8s),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(observable);
    }

    private void WhenWatchStarted()
    {
        _watchKube = new WatchKube(_config,
            _loggerFactoryMock.Object,
            _kubeApiClientMock.Object,
            _serviceBuilderMock.Object,
            _testScheduler);
    }

    private void WhenServicesRequested()
    {
        _resultServices = _watchKube.GetAsync().GetAwaiter().GetResult();
    }

    private void WhenTimePassing()
    {
        _testScheduler.Start();
    }

    private void WhenServiceBuilderMaps()
    {
        _serviceBuilderMock
            .Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns((KubeRegistryConfiguration config, EndpointsV1 endpoints) =>
            {
                return endpoints.Subsets.Select((x, i) => new Service(
                    config.KeyOfServiceInK8s,
                    new ServiceHostAndPort(x.Addresses[i].Hostname, x.Ports[i].Port),
                    i.ToString(),
                    endpoints.ApiVersion,
                    Enumerable.Empty<string>()));
            });
    }

    private void ThenServicesCountIs(int count)
    {
        _resultServices.Count.ShouldBe(count);
    }

    private void AndThenTimePassed(TimeSpan timeSpan)
    {
        _testScheduler.Clock.ShouldBe(timeSpan.Ticks);
    }

    private void AndThenWarningMessageLogged()
    {
        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()));
    }

    private void AndThenErrorMessageLogged()
    {
        _logger.Verify(x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()));
    }

    private IResourceEventV1<EndpointsV1>[] CreateOneEvent(ResourceEventType eventType)
    {
        var resourceEvent = new ResourceEventV1<EndpointsV1>()
        {
            EventType = eventType, 
            Resource = CreateEndpoints(),
        };
        return new IResourceEventV1<EndpointsV1>[] { resourceEvent };
    }

    private EndpointsV1 CreateEndpoints()
    {
        var endpoints = new EndpointsV1
        {
            Kind = "endpoint",
            ApiVersion = "1.0",
            Metadata = new ObjectMetaV1
            {
                Name = _config.KeyOfServiceInK8s,
                Namespace = _config.KubeNamespace,
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
}
