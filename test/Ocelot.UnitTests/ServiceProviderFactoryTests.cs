using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.ServiceDiscovery;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests
{
    public class ServiceProviderFactoryTests
    {
        private ReRoute _reRote;
        private IServiceProvider _result;
        private ServiceProviderFactory _factory;

        public ServiceProviderFactoryTests()
        {
            _factory = new ServiceProviderFactory();
        }
        
        [Fact]
        public void should_return_no_service_provider()
        {
            var reRoute = new ReRouteBuilder()
            .WithDownstreamHost("127.0.0.1")
            .WithDownstreamPort(80)
            .Build();

            this.Given(x => x.GivenTheReRoute(reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<NoServiceProvider>())
                .BDDfy();
        }

        private void GivenTheReRoute(ReRoute reRoute)
        {
            _reRote = reRoute;
        }

        private void WhenIGetTheServiceProvider()
        {
            _result = _factory.Get(_reRote);
        }

        private void ThenTheServiceProviderIs<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }
}