﻿using System;
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
    public class PollingConsulServiceDiscoveryProviderTests : IDisposable
    {
        private IWebHost _fakeConsulBuilder;
        private readonly List<ServiceEntry> _serviceEntries;
        private readonly int _delay;
        private PollingConsulServiceDiscoveryProvider _provider;
        private readonly string _serviceName;
        private readonly int _port;
        private readonly string _consulHost;
        private readonly string _fakeConsulServiceDiscoveryUrl;
        private List<Service> _services;
        private readonly Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IOcelotLogger> _logger;
        private string _receivedToken;
        private IConsulClientFactory _clientFactory;
        private PollingConsulRegistryConfiguration _config;

        public PollingConsulServiceDiscoveryProviderTests()
        {
            _serviceName = "test";
            _port = 8500;
            _consulHost = "localhost";
            _fakeConsulServiceDiscoveryUrl = $"http://{_consulHost}:{_port}";
            _serviceEntries = new List<ServiceEntry>();
            _delay = 0;

            _factory = new Mock<IOcelotLoggerFactory>();
            _clientFactory = new ConsulClientFactory();
            _logger = new Mock<IOcelotLogger>();
            _factory.Setup(x => x.CreateLogger<PollingConsulServiceDiscoveryProvider>()).Returns(_logger.Object);

            _config = new PollingConsulRegistryConfiguration(_consulHost, _port, _serviceName, _receivedToken, _delay);
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
                .When(x => WhenIGetTheServices(1))
                .Then(x => ThenTheCountIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_use_token()
        {
            var token = "test token";
            _config = new PollingConsulRegistryConfiguration(_consulHost, _port, _serviceName, token, _delay);

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

            this.Given(_ => GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(_ => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
                .When(_ => WhenIGetTheServices(1))
                .Then(_ => ThenTheCountIs(1))
                .And(_ => _receivedToken.ShouldBe(token))
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
                .When(x => WhenIGetTheServices(0))
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
                .When(x => WhenIGetTheServices(0))
                .Then(x => ThenTheCountIs(0))
                .And(x => ThenTheLoggerHasBeenCalledCorrectlyForInvalidPorts())
                .BDDfy();
        }

        private void ThenTheLoggerHasBeenCalledCorrectlyForInvalidAddress()
        {
            var result = WaitFor(2000).Until(() => {
                try
                {
                    _logger.Verify(
                        x => x.LogWarning(
                            "Unable to use service Address: http://localhost and Port: 50881 as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0"),
                        Times.AtLeastOnce);

                    _logger.Verify(
                        x => x.LogWarning(
                            "Unable to use service Address: http://localhost and Port: 50888 as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0"),
                        Times.AtLeastOnce);

                    return true;

                }
                catch(Exception)
                {
                    return false;
                }
            });

            result.ShouldBeTrue();
        }

        private void ThenTheLoggerHasBeenCalledCorrectlyForInvalidPorts()
        {
            var result = WaitFor(2000).Until(() => {
                try
                {
                    _logger.Verify(
                        x => x.LogWarning(
                            "Unable to use service Address: localhost and Port: -1 as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0"),
                        Times.AtLeastOnce);

                    _logger.Verify(
                        x => x.LogWarning(
                            "Unable to use service Address: localhost and Port: 0 as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0"),
                        Times.AtLeastOnce);

                    return true;

                }
                catch(Exception)
                {
                    return false;
                }
            });

            result.ShouldBeTrue();
        }

        private void ThenTheCountIs(int count)
        {
            _services.Count.ShouldBe(count);
        }

        private void WhenIGetTheServices(int expected)
        {
            _provider = new PollingConsulServiceDiscoveryProvider(_config, _factory.Object, _clientFactory);

            var result = WaitFor(2000).Until(() => {
                try
                {
                    _services = _provider.Get().GetAwaiter().GetResult();
                    if(_services.Count == expected)
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
                            if (context.Request.Headers.TryGetValue("X-Consul-Token", out var values))
                            {
                                _receivedToken = values.First();
                            }

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
