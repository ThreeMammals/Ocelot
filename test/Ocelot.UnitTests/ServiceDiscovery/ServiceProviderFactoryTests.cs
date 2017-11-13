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
        private ReRoute _reRoute;

        public ServiceProviderFactoryTests()
        {
            _factory = new ServiceDiscoveryProviderFactory();
        }
        
        [Fact]
        public void should_return_no_service_provider()
        {
            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            var reRoute = new ReRouteBuilder().Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConfigurationServiceProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_consul_service_provider()
        {
            var reRoute = new ReRouteBuilder()
                .WithServiceName("product")
                .WithUseServiceDiscovery(true)
                .Build();

            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConsulServiceDiscoveryProvider>())
                .BDDfy();
        }

        private void GivenTheReRoute(ServiceProviderConfiguration serviceConfig, ReRoute reRoute)
        {
            _serviceConfig = serviceConfig;
            _reRoute = reRoute;
        }

        private void WhenIGetTheServiceProvider()
        {
            _result = _factory.Get(_serviceConfig, _reRoute);
        }

        private void ThenTheServiceProviderIs<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }
}