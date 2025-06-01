using KubeClient;
using Microsoft.Extensions.Logging;
using Ocelot.Provider.Kubernetes;

namespace Ocelot.UnitTests.Kubernetes;

public class EndpointClientV1Tests
{
    private readonly EndPointClientV1 _endpointClient;

    public EndpointClientV1Tests()
    {
        Mock<IKubeApiClient> kubeApiClient = new();
        
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        kubeApiClient.Setup(x => x.LoggerFactory)
            .Returns(loggerFactory.Object);
        _endpointClient = new EndPointClientV1(kubeApiClient.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetAsync_WhenServiceIsNullOrEmpty_ThrowsArgumentException(string serviceName)
    {
        // Act
        var watchCall = () => _endpointClient.GetAsync(serviceName, null, CancellationToken.None);

        // Assert
        var e = await watchCall.ShouldThrowAsync<ArgumentNullException>();
        e.ParamName.ShouldBe(nameof(serviceName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Watch_WhenServiceIsNullOrEmpty_ThrowsArgumentException(string serviceName)
    {
        // Act
        var watchCall = () => _endpointClient.Watch(serviceName, null, CancellationToken.None);
        
        // Assert
        watchCall.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe(nameof(serviceName));
    }

    [Fact]
    public void Watch_ProvidesObservable()
    {
        // Act
        var observable = _endpointClient.Watch("some-service", null, CancellationToken.None);
        
        // Assert
        observable.ShouldNotBeNull();
    }
}
