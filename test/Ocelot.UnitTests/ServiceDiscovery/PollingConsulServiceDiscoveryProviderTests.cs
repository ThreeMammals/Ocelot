using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Infrastructure.Consul;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using Xunit;
using TestStack.BDDfy;
using Shouldly;
using static Ocelot.Infrastructure.Wait;

namespace Ocelot.UnitTests.ServiceDiscovery
{
    public class PollingConsulServiceDiscoveryProviderTests
    {
        private readonly int _delay;
        private PollingConsulServiceDiscoveryProvider _provider;
        private readonly string _serviceName;
        private List<Service> _services;
        private readonly Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IOcelotLogger> _logger;
        private Mock<IServiceDiscoveryProvider> _consulServiceDiscoveryProvider;
        private List<Service> _result;

        public PollingConsulServiceDiscoveryProviderTests()
        {
            _services = new List<Service>();
            _delay = 1;
            _factory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _factory.Setup(x => x.CreateLogger<PollingConsulServiceDiscoveryProvider>()).Returns(_logger.Object);
            _consulServiceDiscoveryProvider = new Mock<IServiceDiscoveryProvider>();
        }

        [Fact]
        public void should_return_service_from_consul()
        {
            var service = new Service("", new ServiceHostAndPort("", 0), "", "", new List<string>());

            this.Given(x => GivenConsulReturns(service))
                .When(x => WhenIGetTheServices(1))
                .Then(x => ThenTheCountIs(1))
                .BDDfy();
        }

        private void GivenConsulReturns(Service service)
        {
            _services.Add(service);
            _consulServiceDiscoveryProvider.Setup(x => x.Get()).ReturnsAsync(_services);
        }

        private void ThenTheCountIs(int count)
        {
            _result.Count.ShouldBe(count);
        }

        private void WhenIGetTheServices(int expected)
        {
            _provider = new PollingConsulServiceDiscoveryProvider(_delay, _serviceName, _factory.Object, _consulServiceDiscoveryProvider.Object);

            var result = WaitFor(3000).Until(() => {
                try
                {
                    _result = _provider.Get().GetAwaiter().GetResult();
                    if(_result.Count == expected)
                    {
                        return true;
                    }

                    return false;
                }
                catch(Exception)
                {
                    return false;
                }
            });

            result.ShouldBeTrue();
        }
    }
}
