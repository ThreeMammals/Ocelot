using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ConsulProvider = Ocelot.Provider.Consul.Consul;

namespace Ocelot.UnitTests.Consul;

public sealed class ConsulTests : UnitTest, IDisposable
{
    private readonly int _port;
    private readonly string _consulHost;
    private readonly string _consulScheme;
    private readonly string _fakeConsulServiceDiscoveryUrl;
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
        _port = 8500;
        _consulHost = "localhost";
        _consulScheme = "http";
        _fakeConsulServiceDiscoveryUrl = $"{_consulScheme}://{_consulHost}:{_port}";
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
    }

    private void Arrange([CallerMemberName] string serviceName = null)
    {
        _config = new ConsulRegistryConfiguration(_consulScheme, _consulHost, _port, serviceName, null);
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
        var service1 = GivenService(50881);
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
        var service1 = GivenService(50881);
        _consulServiceEntries.Add(service1.ToServiceEntry());
        GivenThereIsAFakeConsulServiceDiscoveryProvider();
        var config = new ConsulRegistryConfiguration(_consulScheme, _consulHost, _port, nameof(Should_use_token), token);
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
        var service1 = GivenService(50881, "http://localhost");
        var service2 = GivenService(50888, "http://localhost");
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
        var service1 = GivenService(50881).WithAddress(string.Empty);
        var service2 = GivenService(50888).WithAddress(null);
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
        _fakeConsulBuilder = new WebHostBuilder()
            .UseUrls(_fakeConsulServiceDiscoveryUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(_fakeConsulServiceDiscoveryUrl)
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

                        var json = JsonSerializer.Serialize(_consulServiceEntries, JsonSerializerOptionsFactory.Web);
                        context.Response.Headers.Append("Content-Type", "application/json");
                        await context.Response.WriteAsync(json);
                    }
                });
            })
            .Build();
        _fakeConsulBuilder.Start();
    }
}
