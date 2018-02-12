using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery;
using Ocelot.Values;
using Xunit;
using TestStack.BDDfy;
using Shouldly;

namespace Ocelot.UnitTests.ServiceDiscovery
{
    public class ConsulServiceDiscoveryProviderTests : IDisposable
    {
        private IWebHost _fakeConsulBuilder;
        private readonly List<ServiceEntry> _serviceEntries;
        private readonly ConsulServiceDiscoveryProvider _provider;
        private readonly string _serviceName;
        private readonly int _port;
        private readonly string _consulHost;
        private readonly string _fakeConsulServiceDiscoveryUrl;
        private List<Service> _services;
        private Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IOcelotLogger> _logger;

        public ConsulServiceDiscoveryProviderTests()
        {
            _serviceName = "test";
            _port = 8500;
            _consulHost = "localhost";
            _fakeConsulServiceDiscoveryUrl = $"http://{_consulHost}:{_port}";
            _serviceEntries = new List<ServiceEntry>();

            _factory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _factory.Setup(x => x.CreateLogger<ConsulServiceDiscoveryProvider>()).Returns(_logger.Object);

            var config = new ConsulRegistryConfiguration(_consulHost, _port, _serviceName);
            _provider = new ConsulServiceDiscoveryProvider(config, _factory.Object);
        }

        [Fact]
        public void should_return_service_from_consul()
        {
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = _serviceName,
                    Address = "localhost",
                    Port = 50881,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            this.Given(x =>GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(x => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
                .When(x => WhenIGetTheServices())
                .Then(x => ThenTheCountIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_services_with_invalid_address()
        {
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = _serviceName,
                    Address = "http://localhost",
                    Port = 50881,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            var serviceEntryTwo = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = _serviceName,
                    Address = "http://localhost",
                    Port = 50888,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(x => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .When(x => WhenIGetTheServices())
                .Then(x => ThenTheCountIs(0))
                .And(x => ThenTheLoggerHasBeenCalledCorrectlyForInvalidAddress())
                .BDDfy();
        }

        [Fact]
        public void should_not_return_services_with_invalid_port()
        {
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = _serviceName,
                    Address = "localhost",
                    Port = -1,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            var serviceEntryTwo = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = _serviceName,
                    Address = "localhost",
                    Port = 0,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(x => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .When(x => WhenIGetTheServices())
                .Then(x => ThenTheCountIs(0))
                .And(x => ThenTheLoggerHasBeenCalledCorrectlyForInvalidPorts())
                .BDDfy();
        }

        private void ThenTheLoggerHasBeenCalledCorrectlyForInvalidAddress()
        {
            _logger.Verify(
                x => x.LogError(
                    "Unable to use service Address: http://localhost and Port: 50881 as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0"),
                Times.Once);

            _logger.Verify(
                x => x.LogError(
                    "Unable to use service Address: http://localhost and Port: 50888 as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0"),
                Times.Once);
        }

        private void ThenTheLoggerHasBeenCalledCorrectlyForInvalidPorts()
        {
            _logger.Verify(
                x => x.LogError(
                    "Unable to use service Address: localhost and Port: -1 as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0"),
                Times.Once);

            _logger.Verify(
                x => x.LogError(
                    "Unable to use service Address: localhost and Port: 0 as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0"),
                Times.Once);
        }

        private void ThenTheCountIs(int count)
        {
            _services.Count.ShouldBe(count);
        }

        private void WhenIGetTheServices()
        {
            _services = _provider.Get().GetAwaiter().GetResult();
        }

        private void GivenTheServicesAreRegisteredWithConsul(params ServiceEntry[] serviceEntries)
        {
            foreach (var serviceEntry in serviceEntries)
            {
                _serviceEntries.Add(serviceEntry);
            }
        }

        private void GivenThereIsAFakeConsulServiceDiscoveryProvider(string url, string serviceName)
        {
            _fakeConsulBuilder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        if (context.Request.Path.Value == $"/v1/health/service/{serviceName}")
                        {
                            await context.Response.WriteJsonAsync(_serviceEntries);
                        }
                    });
                })
                .Build();

            _fakeConsulBuilder.Start();
        }

        public void Dispose()
        {
            _fakeConsulBuilder?.Dispose();
        }
    }
}
