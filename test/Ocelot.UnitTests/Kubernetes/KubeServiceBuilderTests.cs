using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.UnitTests.Kubernetes;

[Trait("Feat", "1967")]
public sealed class KubeServiceBuilderTests
{
    private readonly Mock<IOcelotLoggerFactory> factory;
    private readonly Mock<IKubeServiceCreator> serviceCreator;
    private readonly Mock<IOcelotLogger> logger;
    private KubeServiceBuilder sut;

    public KubeServiceBuilderTests()
    {
        factory = new();
        serviceCreator = new();
        logger = new();
    }

    private void Arrange()
    {
        factory.Setup(x => x.CreateLogger<KubeServiceBuilder>())
            .Returns(logger.Object)
            .Verifiable();
        logger.Setup(x => x.LogDebug(It.IsAny<Func<string>>()))
            .Verifiable();
        sut = new KubeServiceBuilder(factory.Object, serviceCreator.Object);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Cstor_NullArgs_ThrownException(bool isFactory, bool isServiceCreator)
    {
        // Arrange
        var arg1 = isFactory ? factory.Object : null;
        var arg2 = isServiceCreator ? serviceCreator.Object : null;

        // Act, Assert
        Assert.Throws<ArgumentNullException>(
            arg1 is null ? "factory" : arg2 is null ? "serviceCreator" : string.Empty,
            () => sut = new KubeServiceBuilder(arg1, arg2));
    }

    [Fact]
    public void Cstor_NotNullArgs_ObjCreated()
    {
        // Arrange
        factory.Setup(x => x.CreateLogger<KubeServiceBuilder>()).Verifiable();

        // Act
        sut = new KubeServiceBuilder(factory.Object, serviceCreator.Object);

        // Assert
        Assert.NotNull(sut);
        factory.Verify(x => x.CreateLogger<KubeServiceBuilder>(), Times.Once());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void BuildServices_NullArgs_ThrownException(bool isConfiguration, bool isEndpoint)
    {
        // Arrange
        var arg1 = isConfiguration ? new KubeRegistryConfiguration() : null;
        var arg2 = isEndpoint ? new EndpointsV1() : null;
        Arrange();

        // Act, Assert
        Assert.Throws<ArgumentNullException>(
            arg1 is null ? "configuration" : arg2 is null ? "endpoint" : string.Empty,
            () => _ = sut.BuildServices(arg1, arg2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void BuildServices_WithSubsets_SelectedManyServicesPerSubset(int subsetCount)
    {
        // Arrange
        var configuration = new KubeRegistryConfiguration();
        var endpoint = new EndpointsV1();
        for (int i = 1; i <= subsetCount; i++)
        {
            var subset = new EndpointSubsetV1();
            subset.Addresses.Add(new() { NodeName = "subset" + i, Hostname = i.ToString() });
            endpoint.Subsets.Add(subset);
        }

        serviceCreator.Setup(x => x.Create(configuration, endpoint, It.IsAny<EndpointSubsetV1>()))
            .Returns<KubeRegistryConfiguration, EndpointsV1, EndpointSubsetV1>((c, e, s) =>
            {
                var item = s.Addresses[0];
                int count = int.Parse(item.Hostname);
                var list = new List<Service>(count);
                while (count > 0)
                {
                    var id = count--.ToString();
                    list.Add(new Service($"{item.NodeName}-service{id}", null, id, id, null));
                }

                return list;
            });
        var many = endpoint.Subsets.Sum(s => int.Parse(s.Addresses[0].Hostname));
        Arrange();

        // Act
        var actual = sut.BuildServices(configuration, endpoint);

        // Assert
        Assert.NotNull(actual);
        var l = actual.ToList();
        Assert.Equal(many, l.Count);
        serviceCreator.Verify(x => x.Create(configuration, endpoint, It.IsAny<EndpointSubsetV1>()),
            Times.Exactly(endpoint.Subsets.Count));
        logger.Verify(x => x.LogDebug(It.IsAny<Func<string>>()),
            Times.Once());
    }

    [Theory]
    [InlineData(false, false, false, false, "K8s '?:?:?' endpoint: Total built 0 services.")]
    [InlineData(false, false, false, true, "K8s '?:?:?' endpoint: Total built 0 services.")]
    [InlineData(false, false, true, false, "K8s '?:?:?' endpoint: Total built 0 services.")]
    [InlineData(false, false, true, true, "K8s '?:?:Name' endpoint: Total built 0 services.")]
    [InlineData(false, true, true, true, "K8s '?:ApiVersion:Name' endpoint: Total built 0 services.")]
    [InlineData(true, true, true, true, "K8s 'Kind:ApiVersion:Name' endpoint: Total built 0 services.")]
    public void BuildServices_WithEndpoint_LogDebug(bool hasKind, bool hasApiVersion, bool hasMetadata, bool hasMetadataName, string message)
    {
        // Arrange
        var configuration = new KubeRegistryConfiguration();
        var endpoint = new EndpointsV1()
        {
            Kind = hasKind ? nameof(EndpointsV1.Kind) : null,
            ApiVersion = hasApiVersion ? nameof(EndpointsV1.ApiVersion) : null,
            Metadata = hasMetadata ? new()
            {
                Name = hasMetadataName ? nameof(ObjectMetaV1.Name) : null,
            } : null,
        };
        Arrange();
        string actualMesssage = null;
        logger.Setup(x => x.LogDebug(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(f => actualMesssage = f.Invoke());

        // Act
        var actual = sut.BuildServices(configuration, endpoint);

        // Assert
        Assert.NotNull(actual);
        logger.Verify(x => x.LogDebug(It.IsAny<Func<string>>()),
            Times.Once());
        Assert.NotNull(actualMesssage);
        Assert.Equal(message, actualMesssage);
    }
}
