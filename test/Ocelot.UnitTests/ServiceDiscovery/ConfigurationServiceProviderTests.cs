using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.ServiceDiscovery
{
    public class ConfigurationServiceProviderTests : UnitTest
    {
        private ConfigurationServiceProvider _serviceProvider;
        private List<Service> _result;
        private List<Service> _expected;

        [Fact]
        public void should_return_services()
        {
            var hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);

            var services = new List<Service>
            {
                new("product", hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
            };

            this.Given(x => x.GivenServices(services))
                .When(x => x.WhenIGetTheService())
                .Then(x => x.ThenTheFollowingIsReturned(services))
                .BDDfy();
        }

        private void GivenServices(List<Service> services)
        {
            _expected = services;
        }

        private async Task WhenIGetTheService()
        {
            _serviceProvider = new ConfigurationServiceProvider(_expected);
            _result = await _serviceProvider.GetAsync();
        }

        private void ThenTheFollowingIsReturned(List<Service> services)
        {
            _result[0].HostAndPort.DownstreamHost.ShouldBe(services[0].HostAndPort.DownstreamHost);

            _result[0].HostAndPort.DownstreamPort.ShouldBe(services[0].HostAndPort.DownstreamPort);

            _result[0].Name.ShouldBe(services[0].Name);
        }
    }
}
