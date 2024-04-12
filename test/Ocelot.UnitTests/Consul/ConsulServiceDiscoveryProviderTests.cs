using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Values;
using ConsulProvider = Ocelot.Provider.Consul.Consul;

namespace Ocelot.UnitTests.Consul
{
    public class ConsulServiceDiscoveryProviderTests : UnitTest, IDisposable
    {
        private IWebHost _fakeConsulBuilder;
        private readonly List<ServiceEntry> _serviceEntries;
        private ConsulProvider _provider;
        private readonly string _serviceName;
        private readonly int _port;
        private readonly string _consulHost;
        private readonly string _consulScheme;
        private readonly string _fakeConsulServiceDiscoveryUrl;
        private List<Service> _services;
        private readonly Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IOcelotLogger> _logger;
        private string _receivedToken;
        private readonly IConsulClientFactory _clientFactory;

        public ConsulServiceDiscoveryProviderTests()
        {
            _serviceName = "test";
            _port = 8500;
            _consulHost = "localhost";
            _consulScheme = "http";
            _fakeConsulServiceDiscoveryUrl = $"{_consulScheme}://{_consulHost}:{_port}";
            _serviceEntries = new List<ServiceEntry>();
            _factory = new Mock<IOcelotLoggerFactory>();
            _clientFactory = new ConsulClientFactory();
            _logger = new Mock<IOcelotLogger>();
            _factory.Setup(x => x.CreateLogger<ConsulProvider>()).Returns(_logger.Object);
            _factory.Setup(x => x.CreateLogger<PollConsul>()).Returns(_logger.Object);
            var config = new ConsulRegistryConfiguration(_consulScheme, _consulHost, _port, _serviceName, null);
            _provider = new ConsulProvider(config, _factory.Object, _clientFactory);
        }

        [Fact]
        public void should_return_service_from_consul()
        {
            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = _serviceName,
                    Address = "localhost",
                    Port = 50881,
                    ID = Guid.NewGuid().ToString(),
                    Tags = Array.Empty<string>(),
                },
            };

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(x => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
                .When(x => WhenIGetTheServices())
                .Then(x => ThenTheCountIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_use_token()
        {
            var token = "test token";
            var config = new ConsulRegistryConfiguration(_consulScheme, _consulHost, _port, _serviceName, token);
            _provider = new ConsulProvider(config, _factory.Object, _clientFactory);

            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = _serviceName,
                    Address = "localhost",
                    Port = 50881,
                    ID = Guid.NewGuid().ToString(),
                    Tags = Array.Empty<string>(),
                },
            };

            this.Given(_ => GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(_ => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
                .When(_ => WhenIGetTheServices())
                .Then(_ => ThenTheCountIs(1))
                .And(_ => ThenTheTokenIs(token))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_services_with_invalid_address()
        {
            var serviceEntryOne = GivenService(address: "http://localhost", port: 50881)
                .ToServiceEntry();
            var serviceEntryTwo = GivenService(address: "http://localhost", port: 50888)
                .ToServiceEntry();

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(x => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .When(x => WhenIGetTheServices())
                .Then(x => ThenTheCountIs(0))
                .And(x => ThenTheLoggerHasBeenCalledCorrectlyWithValidationWarning(serviceEntryOne, serviceEntryTwo))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_services_with_empty_address()
        {
            var serviceEntryOne = GivenService(port: 50881)
                .WithAddress(string.Empty)
                .ToServiceEntry();
            var serviceEntryTwo = GivenService(port: 50888)
                .WithAddress(null)
                .ToServiceEntry();

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(x => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .When(x => WhenIGetTheServices())
                .Then(x => ThenTheCountIs(0))
                .And(x => ThenTheLoggerHasBeenCalledCorrectlyWithValidationWarning(serviceEntryOne, serviceEntryTwo))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_services_with_invalid_port()
        {
            var serviceEntryOne = GivenService(port: -1)
                .ToServiceEntry();
            var serviceEntryTwo = GivenService(port: 0)
                .ToServiceEntry();

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(_fakeConsulServiceDiscoveryUrl, _serviceName))
                .And(x => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .When(x => WhenIGetTheServices())
                .Then(x => ThenTheCountIs(0))
                .And(x => ThenTheLoggerHasBeenCalledCorrectlyWithValidationWarning(serviceEntryOne, serviceEntryTwo))
                .BDDfy();
        }

        private AgentService GivenService(string address = null, int? port = null, string id = null, string[] tags = null)
            => new()
            {
                Service = _serviceName,
                Address = address ?? "localhost",
                Port = port ?? 123,
                ID = id ?? Guid.NewGuid().ToString(),
                Tags = tags ?? Array.Empty<string>(),
            };

        private void ThenTheLoggerHasBeenCalledCorrectlyWithValidationWarning(params ServiceEntry[] serviceEntries)
        {
            foreach (var entry in serviceEntries)
            {
                var service = entry.Service;
                var expected = $"Unable to use service address: '{service.Address}' and port: {service.Port} as it is invalid for the service: '{service.Service}'. Address must contain host only e.g. 'localhost', and port must be greater than 0.";
                _logger.Verify(x => x.LogWarning(It.Is<Func<string>>(y => y.Invoke() == expected)), Times.Once);
            }
        }

        private void ThenTheCountIs(int count)
        {
            _services.Count.ShouldBe(count);
        }

        private void WhenIGetTheServices()
        {
            _services = _provider.GetAsync().GetAwaiter().GetResult();
        }

        private void ThenTheTokenIs(string token)
        {
            _receivedToken.ShouldBe(token);
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

                            var json = JsonConvert.SerializeObject(_serviceEntries);
                            context.Response.Headers.Append("Content-Type", "application/json");
                            await context.Response.WriteAsync(json);
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
