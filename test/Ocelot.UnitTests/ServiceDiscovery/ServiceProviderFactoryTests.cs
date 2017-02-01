using Ocelot.ServiceDiscovery;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.ServiceDiscovery
{
    public class ServiceProviderFactoryTests
    {
        private ServiceConfiguraion _serviceConfig;
        private IServiceProvider _result;
        private readonly ServiceProviderFactory _factory;

        public ServiceProviderFactoryTests()
        {
            _factory = new ServiceProviderFactory();
        }
        
        [Fact]
        public void should_return_no_service_provider()
        {
            var serviceConfig = new ServiceConfiguraion("product", "127.0.0.1", 80, false);

            this.Given(x => x.GivenTheReRoute(serviceConfig))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConfigurationServiceProvider>())
                .BDDfy();
        }

        private void GivenTheReRoute(ServiceConfiguraion serviceConfig)
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