using KubeClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Provider.Kubernetes;

namespace Ocelot.UnitTests.Kubernetes;

public sealed class KubeApiClientFactoryTests : KubeApiClientFactoryTestsBase
{
    [Theory]
    [Trait("Bug", "2299")]
    [InlineData(null)]
    [InlineData("")]
    public void ServiceAccountPath_NoValue_FallbackedToDefValue(string serviceAccountPath)
    {
        // Arrange
        var s = new FakeKubeApiClientFactory(serviceAccountPath);

        // Act
        var actual = s.ActualServiceAccountPath;

        // Assert
        actual.ShouldNotBeNullOrEmpty();
        Assert.Equal(KubeClientConstants.DefaultServiceAccountPath, actual);
    }
}

[Collection(nameof(SequentialTests))]
public class KubeApiClientFactorySequentialTests : KubeApiClientFactoryTestsBase
{
    [Fact]
    [Trait("Bug", "2299")]
    public async Task Get_UsePodServiceAccount_ShouldCreateFromPodServiceAccount()
    {
        // Arrange
        var serviceAccountPath = Path.Combine(AppContext.BaseDirectory, TestID);
        var stub = new FakeKubeApiClientFactory(logger.Object, options.Object, serviceAccountPath);
        var expectedHost = IPAddress.Loopback.ToString();
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_HOST", expectedHost);
        int expectedPort = PortFinder.GetRandomPort();
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_PORT", expectedPort.ToString());

        _folders.Add(serviceAccountPath);
        if (!Directory.Exists(serviceAccountPath))
        {
            Directory.CreateDirectory(serviceAccountPath);
        }

        var path = Path.Combine(serviceAccountPath, "namespace");
        await File.WriteAllTextAsync(path, nameof(Get_UsePodServiceAccount_ShouldCreateFromPodServiceAccount));
        _files.Add(path);

        path = Path.Combine(serviceAccountPath, "token");
        await File.WriteAllTextAsync(path, TestID);
        _files.Add(path);

        path = Path.Combine(serviceAccountPath, "ca.crt");
        await FakeKubeApiClientFactory.CreateCertificate(path);
        _files.Add(path);

        var log = new Mock<ILogger>();
        logger.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(log.Object);

        // Act
        const bool UsePodServiceAccount = true;
        var actual = stub.Get(UsePodServiceAccount); // !

        // Assert
        actual.ShouldNotBeNull().ShouldBeOfType<KubeApiClient>();
        actual.ApiEndPoint.ShouldNotBeNull();
        actual.ApiEndPoint.Host.ShouldBe(expectedHost);
        actual.ApiEndPoint.Port.ShouldBe(expectedPort);
        actual.DefaultNamespace.ShouldNotBeNull(nameof(Get_UsePodServiceAccount_ShouldCreateFromPodServiceAccount));
        actual.LoggerFactory.ShouldNotBeNull();
        actual.LoggerFactory.ShouldBe(logger.Object);
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_PORT", null);
    }
}

public class KubeApiClientFactoryTestsBase : FileUnitTest
{
    protected readonly Mock<ILoggerFactory> logger = new();
    protected readonly Mock<IOptions<KubeClientOptions>> options = new();
    protected KubeApiClientFactory sut;

    public KubeApiClientFactoryTestsBase()
    {
        sut = new(logger.Object, options.Object);
    }
}
