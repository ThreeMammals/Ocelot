using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Polling;
using Ocelot.Provider.Consul;
using Ocelot.Values;

namespace Ocelot.UnitTests.Consul;

public class PollingConsulServiceDiscoveryProviderTests
{
    private readonly Mock<Provider.Consul.Consul> _consulServiceDiscoveryProvider;
    private readonly int _delay;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly List<Service> _services;
    private List<Service> _result;

    public PollingConsulServiceDiscoveryProviderTests()
    {
        _services = new List<Service>();
        _delay = 1;
        _factory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _factory.Setup(x => x.CreateLogger<ServicePollingHandler<Provider.Consul.Consul>>()).Returns(_logger.Object);
        _consulServiceDiscoveryProvider = new Mock<Provider.Consul.Consul>();
    }

    [Fact]
    public void should_return_service_from_consul()
    {
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty,
            new List<string>());

        this.Given(x => GivenConsulReturns(service))
            .When(x => WhenIGetTheServices(1))
            .Then(x => ThenTheCountIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_return_service_from_consul_without_delay()
    {
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty,
            new List<string>());

        this.Given(x => GivenConsulReturns(service))
            .When(x => WhenIGetTheServicesWithoutDelay(1))
            .Then(x => ThenTheCountIs(1))
            .BDDfy();
    }

    private void GivenConsulReturns(Service service)
    {
        _services.Add(service);
        _consulServiceDiscoveryProvider.Setup(x => x.Get()).ReturnsAsync(_services);
    }

    private void ThenTheCountIs(int count)
    {
        _result.Count.ShouldBe(count);
    }

    private void WhenIGetTheServices(int expected)
    {
        var provider = new PollConsul(_consulServiceDiscoveryProvider.Object, _delay, "test", _factory.Object);

        var result = Wait.WaitFor(3000).Until(() =>
        {
            try
            {
                _result = provider.Get().GetAwaiter().GetResult();
                return _result.Count == expected;
            }
            catch
            {
                return false;
            }
        });

        result.ShouldBeTrue();
    }

    private void WhenIGetTheServicesWithoutDelay(int expected)
    {
        var provider = new PollConsul(_consulServiceDiscoveryProvider.Object, 0, "test", _factory.Object);

        bool result;
        try
        {
            _result = provider.Get().GetAwaiter().GetResult();
            result = _result.Count == expected;
        }
        catch (Exception)
        {
            result = false;
        }

        result.ShouldBeTrue();
    }
}
