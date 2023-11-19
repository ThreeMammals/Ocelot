using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Polling;
using Ocelot.Provider.Kubernetes;
using Ocelot.Values;

namespace Ocelot.UnitTests.Kubernetes;

public class PollingKubeServiceDiscoveryProviderTests
{
    private readonly int _delay;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<KubernetesServiceDiscoveryProvider> _kubeServiceDiscoveryProvider;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly List<Service> _services;
    private PollKube _provider;
    private List<Service> _result;

    public PollingKubeServiceDiscoveryProviderTests()
    {
        _services = new List<Service>();
        _delay = 1;
        _factory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _factory.Setup(x => x.CreateLogger<ServicePollingHandler<KubernetesServiceDiscoveryProvider>>())
            .Returns(_logger.Object);
        _kubeServiceDiscoveryProvider = new Mock<KubernetesServiceDiscoveryProvider>();
    }

    [Fact]
    public void should_return_service_from_kube()
    {
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty,
            new List<string>());

        this.Given(x => GivenKubeReturns(service))
            .When(x => WhenIGetTheServices(1))
            .Then(x => ThenTheCountIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_return_service_from_consul_without_delay()
    {
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty,
            new List<string>());

        this.Given(x => GivenKubeReturns(service))
            .When(x => WhenIGetTheServicesWithoutDelay(1))
            .Then(x => ThenTheCountIs(1))
            .BDDfy();
    }

    private void GivenKubeReturns(Service service)
    {
        _services.Add(service);
        _kubeServiceDiscoveryProvider.Setup(x => x.GetAsync()).ReturnsAsync(_services);
    }

    private void ThenTheCountIs(int count)
    {
        _result.Count.ShouldBe(count);
    }

    private void WhenIGetTheServices(int expected)
    {
        _provider = new PollKube(_kubeServiceDiscoveryProvider.Object, _delay, "test", _factory.Object);

        var result = Wait.WaitFor(3000).Until(() =>
        {
            try
            {
                _result = _provider.GetAsync().GetAwaiter().GetResult();
                return _result.Count == expected;
            }
            catch (Exception)
            {
                return false;
            }
        });

        result.ShouldBeTrue();
    }

    private void WhenIGetTheServicesWithoutDelay(int expected)
    {
        var provider = new PollKube(_kubeServiceDiscoveryProvider.Object, 0, "test", _factory.Object);
        bool result;

        try
        {
            _result = provider.GetAsync().GetAwaiter().GetResult();
            result = _result.Count == expected;
        }
        catch (Exception)
        {
            result = false;
        }

        result.ShouldBeTrue();
    }
}
