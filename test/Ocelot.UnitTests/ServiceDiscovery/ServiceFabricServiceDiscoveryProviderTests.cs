using Ocelot.ServiceDiscovery.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.ServiceDiscovery
{
    public class ServiceFabricServiceDiscoveryProviderTests : UnitTest
    {
        private ServiceFabricServiceDiscoveryProvider _provider;
        private ServiceFabricConfiguration _config;
        private string _host;
        private string _serviceName;
        private int _port;
        private List<Service> _services;

        [Fact]
        public void should_return_service_fabric_naming_service()
        {
            this.Given(x => GivenTheFollowing())
                .When(x => WhenIGet())
                .Then(x => ThenTheServiceFabricNamingServiceIsRetured())
                .BDDfy();
        }

        private void GivenTheFollowing()
        {
            _host = "localhost";
            _serviceName = "OcelotServiceApplication/OcelotApplicationService";
            _port = 19081;
        }

        private async Task WhenIGet()
        {
            _config = new ServiceFabricConfiguration(_host, _port, _serviceName);
            _provider = new ServiceFabricServiceDiscoveryProvider(_config);
            _services = await _provider.GetAsync();
        }

        private void ThenTheServiceFabricNamingServiceIsRetured()
        {
            _services.Count.ShouldBe(1);
            _services[0].HostAndPort.DownstreamHost.ShouldBe(_host);
            _services[0].HostAndPort.DownstreamPort.ShouldBe(_port);
        }
    }
}
