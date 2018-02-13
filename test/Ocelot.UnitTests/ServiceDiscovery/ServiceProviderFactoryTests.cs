using System;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
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
        private Mock<IOcelotLoggerFactory> _loggerFactory;

        public ServiceProviderFactoryTests()
        {
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _factory = new ServiceDiscoveryProviderFactory(_loggerFactory.Object);
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
        public void should_return_list_of_configuration_services()
        {
            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            var downstreamAddresses = new List<DownstreamHostAndPort>()
            {
                new DownstreamHostAndPort("asdf.com", 80),
                new DownstreamHostAndPort("abc.com", 80)
            };

            var reRoute = new ReRouteBuilder().WithDownstreamAddresses(downstreamAddresses).Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConfigurationServiceProvider>())
                .Then(x => ThenTheFollowingServicesAreReturned(downstreamAddresses))
                .BDDfy();
        }

        private void ThenTheFollowingServicesAreReturned(List<DownstreamHostAndPort> downstreamAddresses)
        {
            var result = (ConfigurationServiceProvider)_result;
            var services = result.Get().Result;
            
            for (int i = 0; i < services.Count; i++)
            {
                var service = services[i];
                var downstreamAddress = downstreamAddresses[i];

                service.HostAndPort.DownstreamHost.ShouldBe(downstreamAddress.Host);
                service.HostAndPort.DownstreamPort.ShouldBe(downstreamAddress.Port);
            }
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
