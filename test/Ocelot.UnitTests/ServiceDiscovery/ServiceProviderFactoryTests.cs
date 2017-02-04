using Ocelot.Configuration;
using Ocelot.ServiceDiscovery;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.ServiceDiscovery
{
    public class ServiceProviderFactoryTests
    {
        private ServiceProviderConfiguraion _serviceConfig;
        private IServiceDiscoveryProvider _result;
        private readonly ServiceDiscoveryProviderFactory _factory;

        public ServiceProviderFactoryTests()
        {
            _factory = new ServiceDiscoveryProviderFactory();
        }
        
        [Fact]
        public void should_return_no_service_provider()
        {
            var serviceConfig = new ServiceProviderConfiguraion("product", "127.0.0.1", 80, false, "Does not matter", string.Empty, 0);

            this.Given(x => x.GivenTheReRoute(serviceConfig))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConfigurationServiceProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_consul_service_provider()
        {
            var serviceConfig = new ServiceProviderConfiguraion("product", string.Empty, 0, true, "Consul", string.Empty, 0);

            this.Given(x => x.GivenTheReRoute(serviceConfig))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConsulServiceDiscoveryProvider>())
                .BDDfy();
        }

        private void GivenTheReRoute(ServiceProviderConfiguraion serviceConfig)
        {
            _serviceConfig = serviceConfig;
        }

        private void WhenIGetTheServiceProvider()
        {
            _result = _factory.Get(_serviceConfig);
        }

        private void ThenTheServiceProviderIs<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }
}