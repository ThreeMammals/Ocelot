using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class DynamicsCreatorTests : UnitTest
{
    private readonly DynamicsCreator _creator;
    private readonly Mock<IRateLimitOptionsCreator> _rloCreator;
    private readonly Mock<IVersionCreator> _versionCreator;
    private readonly Mock<IVersionPolicyCreator> _versionPolicyCreator;
    private readonly Mock<IMetadataCreator> _metadataCreator;
    private IReadOnlyList<Route> _result;
    private FileConfiguration _fileConfig;
    private RateLimitOptions _rlo1;
    private RateLimitOptions _rlo2;
    private Version _version;
    private HttpVersionPolicy _versionPolicy;
    private Dictionary<string, string> _expectedMetadata;

    public DynamicsCreatorTests()
    {
        _versionCreator = new Mock<IVersionCreator>();
        _versionPolicyCreator = new Mock<IVersionPolicyCreator>();
        _metadataCreator = new Mock<IMetadataCreator>();
        _rloCreator = new Mock<IRateLimitOptionsCreator>();
        _creator = new DynamicsCreator(_rloCreator.Object, _versionCreator.Object, _versionPolicyCreator.Object, _metadataCreator.Object);
    }

    [Fact]
    public void Should_return_nothing()
    {
        // Arrange
        _fileConfig = new FileConfiguration();

        // Act
        _result = _creator.Create(_fileConfig);

        // Assert
        _result.Count.ShouldBe(0);

        // Assert: then the RloCreator is not called
        _rloCreator.Verify(x => x.Create(It.IsAny<IRouteRateLimiting>(), It.IsAny<FileGlobalConfiguration>()), Times.Never);

        // Assert: then the metadata creator is not called
        _metadataCreator.Verify(x => x.Create(It.IsAny<IDictionary<string, string>>(), It.IsAny<FileGlobalConfiguration>()), Times.Never);
    }

    [Fact]
    public void Should_return_routes()
    {
        // Arrange
        _fileConfig = new FileConfiguration
        {
            DynamicRoutes = new()
            {
                GivenDynamicRoute("1", false, "1.1", "foo", "bar"),
                GivenDynamicRoute("2", true, "2.0", "foo", "baz"),
            },
        };
        GivenTheRloCreatorReturns();
        GivenTheVersionCreatorReturns();
        GivenTheVersionPolicyCreatorReturns();
        GivenTheMetadataCreatorReturns();

        // Act
        _result = _creator.Create(_fileConfig);

        // Assert
        ThenTheRoutesAreReturned();
        ThenTheRloCreatorIsCalledCorrectly();
        ThenTheVersionCreatorIsCalledCorrectly();
        ThenTheMetadataCreatorIsCalledCorrectly();
    }

    #region PR 2073

    [Fact]
    [Trait("PR", "2073")] // https://github.com/ThreeMammals/Ocelot/pull/2073
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    [Trait("Feat", "1869")] // https://github.com/ThreeMammals/Ocelot/issues/1869
    public void CreateTimeout_HasRouteTimeout_ShouldCreateFromRoute()
    {
        // Arrange
        var route = new FileDynamicRoute { Timeout = 11 };
        var global = new FileGlobalConfiguration { Timeout = 22 };

        // Act
        var timeout = _creator.CreateTimeout(route, global);

        // Assert
        Assert.Equal(route.Timeout, timeout);
    }

    [Fact]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    public void CreateTimeout_NoRouteTimeoutAndHasGlobalOne_ShouldCreateFromGlobalConfig()
    {
        // Arrange
        var route = new FileDynamicRoute();
        var global = new FileGlobalConfiguration { Timeout = 22 };

        // Act
        var timeout = _creator.CreateTimeout(route, global);

        // Assert
        Assert.Null(route.Timeout);
        Assert.Equal(global.Timeout, timeout);
    }

    [Fact]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    public void CreateTimeout_NoRouteTimeoutAndNoGlobalOne_ShouldCreateFromDownstreamRouteDefaults()
    {
        // Arrange
        var route = new FileDynamicRoute();
        var global = new FileGlobalConfiguration();

        // Act
        var timeout = _creator.CreateTimeout(route, global);

        // Assert
        Assert.Null(route.Timeout);
        Assert.Null(global.Timeout);
        Assert.Equal(DownstreamRoute.DefTimeout, timeout);
    }
    #endregion

    private static FileDynamicRoute GivenDynamicRoute(string serviceName, bool enableRateLimiting, string downstreamHttpVersion, string key, string value) => new()
    {
        ServiceName = serviceName,
        RateLimitRule = new FileRateLimitByHeaderRule
        {
            EnableRateLimiting = enableRateLimiting,
        },
        DownstreamHttpVersion = downstreamHttpVersion,
        Metadata = new Dictionary<string, string>
        {
            [key] = value,
        },
    };

    private void ThenTheRloCreatorIsCalledCorrectly()
    {
        _rloCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[0], _fileConfig.GlobalConfiguration),
            Times.Once);
        _rloCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[1], _fileConfig.GlobalConfiguration),
            Times.Once);
    }

    private void ThenTheVersionCreatorIsCalledCorrectly()
    {
        _versionCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[0].DownstreamHttpVersion), Times.Once);
        _versionCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[1].DownstreamHttpVersion), Times.Once);

        _versionPolicyCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[0].DownstreamHttpVersionPolicy), Times.Exactly(2));
        _versionPolicyCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[1].DownstreamHttpVersionPolicy), Times.Exactly(2));
    }

    private void ThenTheMetadataCreatorIsCalledCorrectly()
    {
        _metadataCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[0].Metadata, It.IsAny<FileGlobalConfiguration>()), Times.Once);
        _metadataCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[1].Metadata, It.IsAny<FileGlobalConfiguration>()), Times.Once);
    }

    private void ThenTheRoutesAreReturned()
    {
        _result.Count.ShouldBe(2);
        _result[0].DownstreamRoute[0].RateLimitOptions.EnableRateLimiting.ShouldBeFalse();
        _result[0].DownstreamRoute[0].RateLimitOptions.ShouldBe(_rlo1);
        _result[0].DownstreamRoute[0].DownstreamHttpVersion.ShouldBe(_version);
        _result[0].DownstreamRoute[0].DownstreamHttpVersionPolicy.ShouldBe(_versionPolicy);
        _result[0].DownstreamRoute[0].ServiceName.ShouldBe(_fileConfig.DynamicRoutes[0].ServiceName);

        _result[1].DownstreamRoute[0].RateLimitOptions.EnableRateLimiting.ShouldBeTrue();
        _result[1].DownstreamRoute[0].RateLimitOptions.ShouldBe(_rlo2);
        _result[1].DownstreamRoute[0].DownstreamHttpVersion.ShouldBe(_version);
        _result[1].DownstreamRoute[0].DownstreamHttpVersionPolicy.ShouldBe(_versionPolicy);
        _result[1].DownstreamRoute[0].ServiceName.ShouldBe(_fileConfig.DynamicRoutes[1].ServiceName);
    }

    private void GivenTheVersionCreatorReturns()
    {
        _version = new Version("1.1");
        _versionCreator.Setup(x => x.Create(It.IsAny<string>())).Returns(_version);
    }

    private void GivenTheVersionPolicyCreatorReturns()
    {
        _versionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        _versionPolicyCreator.Setup(x => x.Create(It.IsAny<string>())).Returns(_versionPolicy);
    }

    private void GivenTheMetadataCreatorReturns()
    {
        _expectedMetadata = new()
        {
            ["foo"] = "bar",
        };
        _metadataCreator.Setup(x => x.Create(It.IsAny<IDictionary<string, string>>(), It.IsAny<FileGlobalConfiguration>()))
            .Returns(new MetadataOptions() { Metadata = _expectedMetadata });
    }

    private void GivenTheRloCreatorReturns()
    {
        _rlo1 = new() { EnableRateLimiting = false };
        _rlo2 = new() { EnableRateLimiting = true };

        _rloCreator
            .SetupSequence(x => x.Create(It.IsAny<IRouteRateLimiting>(), It.IsAny<FileGlobalConfiguration>()))
            .Returns(_rlo1)
            .Returns(_rlo2);
    }
}
