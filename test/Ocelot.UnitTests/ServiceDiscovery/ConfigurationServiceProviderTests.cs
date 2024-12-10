using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.ServiceDiscovery;

public class ConfigurationServiceProviderTests : UnitTest
{
    private ConfigurationServiceProvider _serviceProvider;
    private List<Service> _result;
    private List<Service> _expected;

    [Fact]
    public async Task Should_return_services()
    {
        var hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);

        var services = new List<Service>
        {
            new("product", hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
        };

        GivenServices(services);
        _serviceProvider = new ConfigurationServiceProvider(_expected);
        _result = await _serviceProvider.GetAsync();
        ThenTheFollowingIsReturned(services);
    }

    private void GivenServices(List<Service> services)
    {
        _expected = services;
    }

    private void ThenTheFollowingIsReturned(List<Service> services)
    {
        _result[0].HostAndPort.DownstreamHost.ShouldBe(services[0].HostAndPort.DownstreamHost);

        _result[0].HostAndPort.DownstreamPort.ShouldBe(services[0].HostAndPort.DownstreamPort);

        _result[0].Name.ShouldBe(services[0].Name);
    }
}
