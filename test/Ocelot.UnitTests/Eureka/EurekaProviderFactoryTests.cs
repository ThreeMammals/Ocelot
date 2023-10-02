using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Builder;
using Ocelot.Provider.Eureka;
using Steeltoe.Discovery;

namespace Ocelot.UnitTests.Eureka
{
    public class EurekaProviderFactoryTests
    {
        [Fact]
        public void should_not_get()
        {
            var config = new ServiceProviderConfigurationBuilder().Build();
            var sp = new ServiceCollection().BuildServiceProvider();
            Should.Throw<NullReferenceException>(() =>
                EurekaProviderFactory.Get(sp, config, null));
        }

        [Fact]
        public void should_get()
        {
            var config = new ServiceProviderConfigurationBuilder().WithType("eureka").Build();
            var client = new Mock<IDiscoveryClient>();
            var services = new ServiceCollection();
            services.AddSingleton(client.Object);
            var sp = services.BuildServiceProvider();
            var route = new DownstreamRouteBuilder()
                .WithServiceName(string.Empty)
                .Build();
            var provider = EurekaProviderFactory.Get(sp, config, route);
            provider.ShouldBeOfType<Provider.Eureka.Eureka>();
        }
    }
}
