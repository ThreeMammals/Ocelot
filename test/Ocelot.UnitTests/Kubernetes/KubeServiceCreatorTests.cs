using KubeClient.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;

namespace Ocelot.UnitTests.Kubernetes;

[Trait("Feat", "1967")]
public sealed class KubeServiceCreatorTests
{
    private readonly Mock<IOcelotLoggerFactory> factory;
    private readonly Mock<IOcelotLogger> logger;
    private KubeServiceCreator sut;

    public KubeServiceCreatorTests()
    {
        factory = new();
        logger = new();
    }

    private void Arrange()
    {
        factory.Setup(x => x.CreateLogger<KubeServiceCreator>())
            .Returns(logger.Object)
            .Verifiable();
        logger.Setup(x => x.LogDebug(It.IsAny<Func<string>>()))
            .Verifiable();
        sut = new KubeServiceCreator(factory.Object);
    }

    [Fact]
    public void Cstor_NullArg_ThrownException()
    {
        // Arrange, Act, Assert
        Assert.Throws<ArgumentNullException>("factory",
            () => sut = new KubeServiceCreator(null));
    }

    [Fact]
    public void Cstor_NotNullArg_ObjCreated()
    {
        // Arrange
        factory.Setup(x => x.CreateLogger<KubeServiceCreator>()).Verifiable();

        // Act
        sut = new KubeServiceCreator(factory.Object);

        // Assert
        Assert.NotNull(sut);
        factory.Verify(x => x.CreateLogger<KubeServiceCreator>(), Times.Once());
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Create_NullArgs_ReturnedEmpty(bool isConfiguration, bool isEndpoint, bool isSubset)
    {
        // Arrange
        var arg1 = isConfiguration ? new KubeRegistryConfiguration() : null;
        var arg2 = isEndpoint ? new EndpointsV1() : null;
        var arg3 = isSubset ? new EndpointSubsetV1() : null;
        Arrange();

        // Act
        var actual = sut.Create(arg1, arg2, arg3);

        // Assert
        Assert.NotNull(actual);
        Assert.Empty(actual);
    }

    [Fact(DisplayName = "Create: With empty args -> No exceptions during creation")]
    public void Create_NotNullButEmptyArgs_CreatedEmptyService()
    {
        // Arrange
        var arg1 = new KubeRegistryConfiguration()
        {
             KubeNamespace = nameof(KubeServiceCreatorTests),
             KeyOfServiceInK8s = nameof(Create_NotNullButEmptyArgs_CreatedEmptyService),
        };
        var arg2 = new EndpointsV1();
        var arg3 = new EndpointSubsetV1();
        arg3.Addresses.Add(new());
        arg2.Subsets.Add(arg3);
        Arrange();

        // Act
        var actual = sut.Create(arg1, arg2, arg3);

        // Assert
        Assert.NotNull(actual);
        Assert.NotEmpty(actual);
        var actualService = actual.SingleOrDefault();
        Assert.NotNull(actualService);
        Assert.Null(actualService.Name);
    }

    [Fact]
    public void Create_ValidArgs_HappyPath()
    {
        // Arrange
        var arg1 = new KubeRegistryConfiguration()
        {
            KubeNamespace = nameof(KubeServiceCreatorTests),
            KeyOfServiceInK8s = nameof(Create_ValidArgs_HappyPath),
            Scheme = "happy", //nameof(HttpScheme.Http),
        };
        var arg2 = new EndpointsV1()
        {
            ApiVersion = "v1",
            Metadata = new()
            {
                Namespace = nameof(KubeServiceCreatorTests),
                Name = nameof(Create_ValidArgs_HappyPath),
                Uid = Guid.NewGuid().ToString(),
            },
        };
        var arg3 = new EndpointSubsetV1();
        arg3.Addresses.Add(new()
        {
            Ip = "8.8.8.8",
            NodeName = "google",
            Hostname = "dns.google",
        });
        var ports = new List<EndpointPortV1>
        {
            new() { Name = nameof(HttpScheme.Http), Port = 80 },
            new() { Name = "happy", Port = 888 },
        };
        arg3.Ports.AddRange(ports);
        arg2.Subsets.Add(arg3);
        Arrange();

        // Act
        var actual = sut.Create(arg1, arg2, arg3);

        // Assert
        Assert.NotNull(actual);
        Assert.NotEmpty(actual);
        var service = actual.SingleOrDefault();
        Assert.NotNull(service);
        Assert.Equal(nameof(Create_ValidArgs_HappyPath), service.Name);
        Assert.Equal("happy", service.HostAndPort.Scheme);
        Assert.Equal(888, service.HostAndPort.DownstreamPort);
        Assert.Equal("8.8.8.8", service.HostAndPort.DownstreamHost);
        logger.Verify(x => x.LogDebug(It.IsAny<Func<string>>()),
            Times.Once());
    }
}
