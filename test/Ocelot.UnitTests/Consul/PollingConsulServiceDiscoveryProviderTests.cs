using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.Consul;

public class PollingConsulServiceDiscoveryProviderTests : UnitTest
{
    private readonly int _delay;
    private readonly List<Service> _services;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IServiceDiscoveryProvider> _consulServiceDiscoveryProvider;
    private List<Service> _result;

    public PollingConsulServiceDiscoveryProviderTests()
    {
        _services = new List<Service>();
        _delay = 1;
        _factory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _factory.Setup(x => x.CreateLogger<PollConsul>()).Returns(_logger.Object);
        _consulServiceDiscoveryProvider = new Mock<IServiceDiscoveryProvider>();
    }

    [Fact]
    public void Should_return_service_from_consul()
    {
        // Arrange
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());
        GivenConsulReturns(service);

        // Act
        WhenIGetTheServices(1);

        // Assert
        _result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_return_service_from_consul_without_delay()
    {
        // Arrange
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());
        GivenConsulReturns(service);

        // Act
        await WhenIGetTheServicesWithoutDelay(1);

        // Assert
        _result.Count.ShouldBe(1);
    }

    private void GivenConsulReturns(Service service)
    {
        _services.Add(service);
        _consulServiceDiscoveryProvider.Setup(x => x.GetAsync()).ReturnsAsync(_services);
    }

    private void WhenIGetTheServices(int expected)
    {
        var provider = new PollConsul(_delay, "test", _factory.Object, _consulServiceDiscoveryProvider.Object);
        var result = Wait.For(3_000).Until(() =>
        {
            try
            {
                _result = provider.GetAsync().GetAwaiter().GetResult();
                return _result.Count == expected;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    private async Task WhenIGetTheServicesWithoutDelay(int expected)
    {
        var provider = new PollConsul(_delay, "test2", _factory.Object, _consulServiceDiscoveryProvider.Object);
        bool result;
        try
        {
            _result = await provider.GetAsync();
            result = _result.Count == expected;
        }
        catch (Exception)
        {
            result = false;
        }

        result.ShouldBeTrue();
    }
}
