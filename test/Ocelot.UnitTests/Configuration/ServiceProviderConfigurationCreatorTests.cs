using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class ServiceProviderConfigurationCreatorTests : UnitTest
{
    private readonly ServiceProviderConfigurationCreator _creator = new();

    [Fact]
    public void Should_create_service_provider_config()
    {
        // Arrange
        var globalConfig = new FileGlobalConfiguration
        {
            ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
            {
                Scheme = "https",
                Host = "127.0.0.1",
                Port = 1234,
                Type = "ServiceFabric",
                Token = "testtoken",
                ConfigurationKey = "woo",
                Namespace = "default",
            },
        };
        var expected = new ServiceProviderConfigurationBuilder()
            .WithScheme("https")
            .WithHost("127.0.0.1")
            .WithPort(1234)
            .WithType("ServiceFabric")
            .WithToken("testtoken")
            .WithConfigurationKey("woo")
            .WithNamespace("default")
            .Build();

        // Act
        var result = _creator.Create(globalConfig);

        // Assert
        result.Scheme.ShouldBe(expected.Scheme);
        result.Host.ShouldBe(expected.Host);
        result.Port.ShouldBe(expected.Port);
        result.Token.ShouldBe(expected.Token);
        result.Type.ShouldBe(expected.Type);
        result.Namespace.ShouldBe(expected.Namespace);
        result.ConfigurationKey.ShouldBe(expected.ConfigurationKey);
    }
}
