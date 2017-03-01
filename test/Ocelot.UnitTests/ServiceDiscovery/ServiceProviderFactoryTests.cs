using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.ServiceDiscovery;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.ServiceDiscovery
{
    public class ServiceProviderFactoryTests
    {
        private ServiceProviderConfiguration _serviceConfig;
        private IServiceDiscoveryProvider _result;
        private readonly ServiceDiscoveryProviderFactory _factory;

        public ServiceProviderFactoryTests()
        {
            _factory = new ServiceDiscoveryProviderFactory();
        }
        
        [Fact]
        public void should_return_no_service_provider()
        {
            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .WithDownstreamHost("127.0.0.1")
                .WithDownstreamPort(80)
                .WithUseServiceDiscovery(false)
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConfigurationServiceProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_consul_service_provider()
        {
            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .WithServiceName("product")
                .WithUseServiceDiscovery(true)
                .WithServiceDiscoveryProvider("Consul")
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConsulServiceDiscoveryProvider>())
                .BDDfy();
        }

        private void GivenTheReRoute(ServiceProviderConfiguration serviceConfig)
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