using Ocelot.Values;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery;
using _Eureka_ = Ocelot.Provider.Eureka.Eureka;

namespace Ocelot.UnitTests.Eureka;

public class EurekaServiceDiscoveryProviderTests : UnitTest
{
    private readonly _Eureka_ _provider;
    private readonly Mock<IDiscoveryClient> _client;
    private readonly string _serviceId;
    private List<Service> _result;

    public EurekaServiceDiscoveryProviderTests()
    {
        _serviceId = "Laura";
        _client = new Mock<IDiscoveryClient>();
        _provider = new _Eureka_(_serviceId, _client.Object);
    }

    [Fact]
    public async Task Should_return_empty_services()
    {
        // Arrange, Act
        _result = await _provider.GetAsync();

        // Assert
        _result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Should_return_service_from_client()
    {
        // Arrange
        var instances = new List<IServiceInstance>
        {
            new EurekaService(_serviceId, "somehost", 801, false, new Uri("http://somehost:801"), new Dictionary<string, string>()),
        };
        _client.Setup(x => x.GetInstances(It.IsAny<string>())).Returns(instances);

        // Act
        _result = await _provider.GetAsync();
        _result.Count.ShouldBe(1);

        // Assert
        _client.Verify(x => x.GetInstances(_serviceId), Times.Once);

        // Assert: Then The Service Is Mapped
        _result[0].HostAndPort.DownstreamHost.ShouldBe("somehost");
        _result[0].HostAndPort.DownstreamPort.ShouldBe(801);
        _result[0].Name.ShouldBe(_serviceId);
    }

    [Fact]
    public async Task Should_return_services_from_client()
    {
        // Arrange
        var instances = new List<IServiceInstance>
        {
            new EurekaService(_serviceId, "somehost", 801, false, new Uri("http://somehost:801"), new Dictionary<string, string>()),
            new EurekaService(_serviceId, "somehost", 801, false, new Uri("http://somehost:801"), new Dictionary<string, string>()),
        };
        _client.Setup(x => x.GetInstances(It.IsAny<string>())).Returns(instances);

        // Act
        _result = await _provider.GetAsync();

        // Assert
        _result.Count.ShouldBe(2);
        _client.Verify(x => x.GetInstances(_serviceId), Times.Once);
    }
}

public class EurekaService : IServiceInstance
{
    public EurekaService(string serviceId, string host, int port, bool isSecure, Uri uri, IDictionary<string, string> metadata)
    {
        ServiceId = serviceId;
        Host = host;
        Port = port;
        IsSecure = isSecure;
        Uri = uri;
        Metadata = metadata;
    }

    public string ServiceId { get; }
    public string Host { get; }
    public int Port { get; }
    public bool IsSecure { get; }
    public Uri Uri { get; }
    public IDictionary<string, string> Metadata { get; }
}
