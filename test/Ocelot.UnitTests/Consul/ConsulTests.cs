using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using System.Runtime.CompilerServices;
using ConsulProvider = Ocelot.Provider.Consul.Consul;

namespace Ocelot.UnitTests.Consul;

/// <summary>
/// TODO Move to integration tests.
/// </summary>
[Collection(nameof(SequentialTests))]
public class ConsulTests : UnitTest, IDisposable
{
    private readonly int _consulPort;
    private readonly string _consulHost;
    private readonly string _consulScheme;
    private readonly List<ServiceEntry> _consulServiceEntries;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IHttpContextAccessor> _contextAccessor;
    private IConsulClientFactory _clientFactory;
    private IConsulServiceBuilder _serviceBuilder;
    private ConsulRegistryConfiguration _config;
    private IWebHost _fakeConsulBuilder;
    private ConsulProvider _provider;
    private string _receivedToken;

    public ConsulTests()
    {
        _consulPort = PortFinder.GetRandomPort();
        _consulHost = "localhost";
        _consulScheme = Uri.UriSchemeHttp;
        _consulServiceEntries = new List<ServiceEntry>();
        _factory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _contextAccessor = new Mock<IHttpContextAccessor>();
        _factory.Setup(x => x.CreateLogger<ConsulProvider>()).Returns(_logger.Object);
        _factory.Setup(x => x.CreateLogger<PollConsul>()).Returns(_logger.Object);
        _factory.Setup(x => x.CreateLogger<DefaultConsulServiceBuilder>()).Returns(_logger.Object);
    }

    public void Dispose()
    {
        _fakeConsulBuilder?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Arrange([CallerMemberName] string serviceName = null)
    {
        _config = new ConsulRegistryConfiguration(_consulScheme, _consulHost, _consulPort, serviceName, null);
        var context = new DefaultHttpContext();
        context.Items.Add(nameof(ConsulRegistryConfiguration), _config);
        _contextAccessor.SetupGet(x => x.HttpContext).Returns(context);
        _clientFactory = new ConsulClientFactory();
        _serviceBuilder = new DefaultConsulServiceBuilder(_contextAccessor.Object, _clientFactory, _factory.Object);
        _provider = new ConsulProvider(_config, _factory.Object, _clientFactory, _serviceBuilder);
    }

    [Fact]
    public async Task Should_return_service_from_consul()
    {
        Arrange();
        var service1 = GivenService(PortFinder.GetRandomPort());
        _consulServiceEntries.Add(service1.ToServiceEntry());
        GivenThereIsAFakeConsulServiceDiscoveryProvider();

        // Act
        var actual = await _provider.GetAsync();

        // Assert
        actual.ShouldNotBeNull().Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_use_token()
    {
        Arrange();
        const string token = "test token";
        var service1 = GivenService(PortFinder.GetRandomPort());
        _consulServiceEntries.Add(service1.ToServiceEntry());
        GivenThereIsAFakeConsulServiceDiscoveryProvider();
        var config = new ConsulRegistryConfiguration(_consulScheme, _consulHost, _consulPort, nameof(Should_use_token), token);
        _provider = new ConsulProvider(config, _factory.Object, _clientFactory, _serviceBuilder);

        // Act
        var actual = await _provider.GetAsync();

        // Assert
        actual.ShouldNotBeNull().Count.ShouldBe(1);
        _receivedToken.ShouldBe(token);
    }

    [Fact]
    public async Task Should_not_return_services_with_invalid_address()
    {
        Arrange();
        var service1 = GivenService(PortFinder.GetRandomPort(), "http://localhost");
        var service2 = GivenService(PortFinder.GetRandomPort(), "http://localhost");
        _consulServiceEntries.Add(service1.ToServiceEntry());
        _consulServiceEntries.Add(service2.ToServiceEntry());
        GivenThereIsAFakeConsulServiceDiscoveryProvider();

        // Act
        var actual = await _provider.GetAsync();

        // Assert
        actual.ShouldNotBeNull().Count.ShouldBe(0);
        ThenTheLoggerHasBeenCalledCorrectlyWithValidationWarning();
    }

    [Fact]
    public async Task Should_not_return_services_with_empty_address()
    {
        Arrange();
        var service1 = GivenService(PortFinder.GetRandomPort()).WithAddress(string.Empty);
        var service2 = GivenService(PortFinder.GetRandomPort()).WithAddress(null);
        _consulServiceEntries.Add(service1.ToServiceEntry());
        _consulServiceEntries.Add(service2.ToServiceEntry());
        GivenThereIsAFakeConsulServiceDiscoveryProvider();

        // Act
        var actual = await _provider.GetAsync();

        // Assert
        actual.ShouldNotBeNull().Count.ShouldBe(0);
        ThenTheLoggerHasBeenCalledCorrectlyWithValidationWarning();
    }

    [Fact]
    public async Task Should_not_return_services_with_invalid_port()
    {
        Arrange();
        var service1 = GivenService(-1);
        var service2 = GivenService(0);
        _consulServiceEntries.Add(service1.ToServiceEntry());
        _consulServiceEntries.Add(service2.ToServiceEntry());
        GivenThereIsAFakeConsulServiceDiscoveryProvider();

        // Act
        var actual = await _provider.GetAsync();

        // Assert
        actual.ShouldNotBeNull().Count.ShouldBe(0);
        ThenTheLoggerHasBeenCalledCorrectlyWithValidationWarning();
    }

    [Fact]
    public async Task GetAsync_NoEntries_ShouldLogWarning()
    {
        Arrange();
        _consulServiceEntries.Clear(); // NoEntries
        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>())).Verifiable();
        GivenThereIsAFakeConsulServiceDiscoveryProvider();

        // Act
        var actual = await _provider.GetAsync();

        // Assert
        actual.ShouldNotBeNull().ShouldBeEmpty();
        var expected = $"Consul Provider: No service entries found for '{nameof(GetAsync_NoEntries_ShouldLogWarning)}' service!";
        _logger.Verify(x => x.LogWarning(It.Is<Func<string>>(y => y.Invoke() == expected)), Times.Once);
    }

    private static AgentService GivenService(int port, string address = null, string id = null, string[] tags = null, [CallerMemberName] string serviceName = null) => new()
    {
        Service = serviceName,
        Address = address ?? "localhost",
        Port = port,
        ID = id ?? Guid.NewGuid().ToString(),
        Tags = tags ?? Array.Empty<string>(),
    };

    private void ThenTheLoggerHasBeenCalledCorrectlyWithValidationWarning()
    {
        foreach (var entry in _consulServiceEntries)
        {
            var service = entry.Service;
            var expected = $"Unable to use service address: '{service.Address}' and port: {service.Port} as it is invalid for the service: '{service.Service}'. Address must contain host only e.g. 'localhost', and port must be greater than 0.";
            _logger.Verify(x => x.LogWarning(It.Is<Func<string>>(y => y.Invoke() == expected)), Times.Once);
        }
    }

    private void GivenThereIsAFakeConsulServiceDiscoveryProvider([CallerMemberName] string serviceName = "test")
    {
        string url = $"{_consulScheme}://{_consulHost}:{_consulPort}";
        _fakeConsulBuilder = TestHostBuilder.Create()
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

                        var json = JsonConvert.SerializeObject(_consulServiceEntries);
                        context.Response.Headers.Append("Content-Type", "application/json");
                        await context.Response.WriteAsync(json);
                    }
                });
            })
            .Build();
        _fakeConsulBuilder.Start(); // problematic starting in case of parallel running of unit tests because of failing of port binding
    }
}
