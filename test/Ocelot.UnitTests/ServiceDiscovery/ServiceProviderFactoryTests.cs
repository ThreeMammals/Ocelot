using System;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Infrastructure.Consul;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.ServiceDiscovery
{
    using Pivotal.Discovery.Client;
    using Steeltoe.Common.Discovery;

    public class ServiceProviderFactoryTests
    {
        private ServiceProviderConfiguration _serviceConfig;
        private IServiceDiscoveryProvider _result;
        private readonly ServiceDiscoveryProviderFactory _factory;
        private DownstreamReRoute _reRoute;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IDiscoveryClient> _discoveryClient;
        private Mock<IOcelotLogger> _logger;

        public ServiceProviderFactoryTests()
        {
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<PollingConsulServiceDiscoveryProvider>()).Returns(_logger.Object);
            _discoveryClient = new Mock<IDiscoveryClient>();
            var consulClient = new Mock<IConsulClientFactory>();
            _factory = new ServiceDiscoveryProviderFactory(_loggerFactory.Object, consulClient.Object, _discoveryClient.Object);
        }
        
        [Fact]
        public void should_return_no_service_provider()
        {
            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            var reRoute = new DownstreamReRouteBuilder().Build();

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

            var reRoute = new DownstreamReRouteBuilder().WithDownstreamAddresses(downstreamAddresses).Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConfigurationServiceProvider>())
                .Then(x => ThenTheFollowingServicesAreReturned(downstreamAddresses))
                .BDDfy();
        }

        [Fact]
        public void should_return_consul_service_provider()
        {
            var reRoute = new DownstreamReRouteBuilder()
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

        [Fact]
        public void should_return_polling_consul_service_provider()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithServiceName("product")
                .WithUseServiceDiscovery(true)
                .Build();

            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .WithType("PollConsul")
                .WithPollingInterval(100000)
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<PollingConsulServiceDiscoveryProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_service_fabric_provider()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithServiceName("product")
                .WithUseServiceDiscovery(true)
                .Build();

            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ServiceFabricServiceDiscoveryProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_eureka_provider()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithServiceName("product")
                .WithUseServiceDiscovery(true)
                .Build();

            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .WithType("Eureka")
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<EurekaServiceDiscoveryProvider>())
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

        private void GivenTheReRoute(ServiceProviderConfiguration serviceConfig, DownstreamReRoute reRoute)
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
