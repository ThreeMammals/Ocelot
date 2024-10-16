using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.Consul
{
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
        public void should_return_service_from_consul()
        {
            var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());

            this.Given(x => GivenConsulReturns(service))
                .When(x => WhenIGetTheServices(1))
                .Then(x => ThenTheCountIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_return_service_from_consul_without_delay()
        {
            var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());

            this.Given(x => GivenConsulReturns(service))
                .When(x => WhenIGetTheServicesWithoutDelay(1))
                .Then(x => ThenTheCountIs(1))
                .BDDfy();
        }

        private void GivenConsulReturns(Service service)
        {
            _services.Add(service);
            _consulServiceDiscoveryProvider.Setup(x => x.GetAsync()).ReturnsAsync(_services);
        }

        private void ThenTheCountIs(int count)
        {
            _result.Count.ShouldBe(count);
        }

        private void WhenIGetTheServices(int expected)
        {
            var provider = new PollConsul(_delay, "test", _factory.Object, _consulServiceDiscoveryProvider.Object);
            var result = Wait.WaitFor(3000).Until(() =>
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
}
