using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.ServiceDiscovery;

public class ServiceDiscoveryProviderFactoryTests : UnitTest
{
    private Response<IServiceDiscoveryProvider> _result;
    private ServiceDiscoveryProviderFactory _factory;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private IServiceProvider _provider;
    private readonly IServiceCollection _collection;

    public ServiceDiscoveryProviderFactoryTests()
    {
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _collection = new ServiceCollection();
        _provider = _collection.BuildServiceProvider(true);
        _factory = new ServiceDiscoveryProviderFactory(_loggerFactory.Object, _provider);

        _loggerFactory.Setup(x => x.CreateLogger<ServiceDiscoveryProviderFactory>())
            .Returns(_logger.Object);
    }

    [Fact]
    public void Should_return_no_service_provider()
    {
        // Arrange
        var serviceConfig = new ServiceProviderConfigurationBuilder()
            .Build();
        var route = new DownstreamRouteBuilder().Build();

        // Act
        WhenIGetTheServiceProvider(serviceConfig, route);

        // Assert
        _result.Data.ShouldBeOfType<ConfigurationServiceProvider>();
    }

    [Fact]
    public async Task Should_return_list_of_configuration_services()
    {
        // Arrange
        var serviceConfig = new ServiceProviderConfigurationBuilder()
            .Build();
        var downstreamAddresses = new List<DownstreamHostAndPort>
        {
            new("asdf.com", 80),
            new("abc.com", 80),
        };
        var route = new DownstreamRouteBuilder().WithDownstreamAddresses(downstreamAddresses).Build();

        // Act
        WhenIGetTheServiceProvider(serviceConfig, route);

        // Assert
        _result.Data.ShouldBeOfType<ConfigurationServiceProvider>();

        // Assert: Then The Following Services Are Returned
        var result = (ConfigurationServiceProvider)_result.Data;
        var services = await result.GetAsync();
        for (var i = 0; i < services.Count; i++)
        {
            var service = services[i];
            var downstreamAddress = downstreamAddresses[i];

            service.HostAndPort.DownstreamHost.ShouldBe(downstreamAddress.Host);
            service.HostAndPort.DownstreamPort.ShouldBe(downstreamAddress.Port);
        }
    }

    [Fact]
    public void Should_return_provider_because_type_matches_reflected_type_from_delegate()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithServiceName("product")
            .Build();
        var serviceConfig = new ServiceProviderConfigurationBuilder()
            .WithType(nameof(Fake))
            .Build();
        GivenAFakeDelegate();

        // Act
        WhenIGetTheServiceProvider(serviceConfig, route);

        // Assert
        _result.Data.GetType().Name.ShouldBe("Fake");
    }

    [Fact]
    public void Should_not_return_provider_because_type_doesnt_match_reflected_type_from_delegate()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithServiceName("product")
            .Build();
        var serviceConfig = new ServiceProviderConfigurationBuilder()
            .WithType("Wookie")
            .Build();
        GivenAFakeDelegate();

        // Act
        WhenIGetTheServiceProvider(serviceConfig, route);

        // Assert
        _result.IsError.ShouldBeTrue();
        _result.Errors.Count.ShouldBe(1);

        _logInformationMessages.ShouldNotBeNull()
            .Count.ShouldBe(2);
        _logger.Verify(x => x.LogInformation(It.IsAny<Func<string>>()),
            Times.Exactly(2));

        _logWarningMessages.ShouldNotBeNull()
            .Count.ShouldBe(1);
        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Once());
    }

    [Fact]
    public void Should_return_service_fabric_provider()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithServiceName("product")
            .Build();
        var serviceConfig = new ServiceProviderConfigurationBuilder()
            .WithType("ServiceFabric")
            .Build();
        GivenAFakeDelegate();

        // Act
        WhenIGetTheServiceProvider(serviceConfig, route);

        // Assert
        _result.Data.ShouldBeOfType<ServiceFabricServiceDiscoveryProvider>();
    }

    [Theory]
    [Trait("Bug", "1954")]
    [InlineData("Kube", true)]
    [InlineData("kube", true)]
    [InlineData("PollKube", true)]
    [InlineData("pollkube", true)]
    [InlineData("unknown", false)]
    public void Should_return_Kubernetes_provider_with_type_names_from_docs(string typeName, bool success)
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithServiceName(TestName())
            .Build();
        var serviceConfig = new ServiceProviderConfigurationBuilder()
            .WithType(typeName)
            .WithPollingInterval(Timeout.Infinite)
            .Build();

        // Arrange: Given Kubernetes Provider
        var k8sClient = new Mock<IKubeApiClient>();
        _collection
            .AddSingleton(KubernetesProviderFactory.Get)
            .AddSingleton(k8sClient.Object)
            .AddSingleton(_loggerFactory.Object);
        _provider = _collection.BuildServiceProvider(true);
        _factory = new ServiceDiscoveryProviderFactory(_loggerFactory.Object, _provider);

        // Act
        WhenIGetTheServiceProvider(serviceConfig, route);

        // Assert
        if (success)
        {
            _result.ShouldBeOfType<OkResponse<IServiceDiscoveryProvider>>();
        }
        else
        {
            _result.ShouldBeOfType<ErrorResponse<IServiceDiscoveryProvider>>();
        }
    }

    private void GivenAFakeDelegate()
    {
        static IServiceDiscoveryProvider fake(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute name) => new Fake();
        _collection.AddSingleton((ServiceDiscoveryFinderDelegate)fake);
        _provider = _collection.BuildServiceProvider(true);
        _factory = new ServiceDiscoveryProviderFactory(_loggerFactory.Object, _provider);
    }

    private class Fake : IServiceDiscoveryProvider
    {
        public Task<List<Service>> GetAsync() => null;
    }

    private readonly List<string> _logInformationMessages = new();
    private readonly List<string> _logWarningMessages = new();

    private void WhenIGetTheServiceProvider(ServiceProviderConfiguration serviceConfig, DownstreamRoute route)
    {
        _logger.Setup(x => x.LogInformation(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(myFunc => _logInformationMessages.Add(myFunc.Invoke()));
        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(myFunc => _logWarningMessages.Add(myFunc.Invoke()));

        _result = _factory.Get(serviceConfig, route);
    }
}
