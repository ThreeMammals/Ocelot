using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class DynamicRoutesCreatorTests : UnitTest
{
    private readonly DynamicRoutesCreator _creator;
    private readonly Mock<IRouteKeyCreator> _lbKeyCreator = new();
    private readonly Mock<IHttpHandlerOptionsCreator> _hhoCreator = new();
    private readonly Mock<ILoadBalancerOptionsCreator> _lboCreator = new();
    private readonly Mock<IQoSOptionsCreator> _qosCreator = new();
    private readonly Mock<IRateLimitOptionsCreator> _rloCreator = new();
    private readonly Mock<IVersionCreator> _versionCreator = new();
    private readonly Mock<IVersionPolicyCreator> _versionPolicyCreator = new();
    private readonly Mock<IMetadataCreator> _metadataCreator = new();
    private readonly Mock<ICacheOptionsCreator> _cacheCreator = new();
    private readonly Mock<IAuthenticationOptionsCreator> _authCreator = new();
    private IReadOnlyList<Route> _result;
    private FileConfiguration _fileConfig;
    private RateLimitOptions[] _rlo;
    private Version _version;
    private HttpVersionPolicy _versionPolicy;
    private Dictionary<string, string> _expectedMetadata;

    public DynamicRoutesCreatorTests()
    {
        _creator = new DynamicRoutesCreator(
            _authCreator.Object,
            _cacheCreator.Object,
            _hhoCreator.Object,
            _lboCreator.Object,
            _metadataCreator.Object, _qosCreator.Object,
            _rloCreator.Object,
            _lbKeyCreator.Object,
            _versionCreator.Object,
            _versionPolicyCreator.Object);
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
        _lbKeyCreator.Verify(x => x.Create(It.IsAny<FileDynamicRoute>(), It.IsAny<LoadBalancerOptions>()), Times.Never);
        _lboCreator.Verify(x => x.Create(It.IsAny<FileDynamicRoute>(), It.IsAny<FileGlobalConfiguration>()), Times.Never);
        _rloCreator.Verify(x => x.Create(It.IsAny<IRouteRateLimiting>(), It.IsAny<FileGlobalConfiguration>()), Times.Never);
        _metadataCreator.Verify(x => x.Create(It.IsAny<IDictionary<string, string>>(), It.IsAny<FileGlobalConfiguration>()), Times.Never);
        _cacheCreator.Verify(x => x.Create(It.IsAny<FileDynamicRoute>(), It.IsAny<FileGlobalConfiguration>(), It.IsAny<string>()), Times.Never);
        _authCreator.Verify(x => x.Create(It.IsAny<FileDynamicRoute>(), It.IsAny<FileGlobalConfiguration>()), Times.Never);
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
        ThenTheBasicCreatorsAreCalledCorrectly();
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

    private void ThenTheBasicCreatorsAreCalledCorrectly()
    {
        _fileConfig.DynamicRoutes.ForEach(dynamicRoute =>
        {
            _authCreator.Verify(x => x.Create(dynamicRoute, _fileConfig.GlobalConfiguration), Times.Once);
            _lbKeyCreator.Verify(x => x.Create(dynamicRoute, It.IsAny<LoadBalancerOptions>()), Times.Once);
            _lboCreator.Verify(x => x.Create(dynamicRoute, _fileConfig.GlobalConfiguration), Times.Once);
            _rloCreator.Verify(x => x.Create(dynamicRoute, _fileConfig.GlobalConfiguration), Times.Once);
            _metadataCreator.Verify(x => x.Create(dynamicRoute.Metadata, _fileConfig.GlobalConfiguration), Times.Once);
            _versionCreator.Verify(x => x.Create(dynamicRoute.DownstreamHttpVersion), Times.Once);
            _versionPolicyCreator.Verify(x => x.Create(dynamicRoute.DownstreamHttpVersionPolicy), Times.Exactly(2));
        });
    }

    private void ThenTheRoutesAreReturned()
    {
        _result.Count.ShouldBe(2);
        for (int i = 0; i < _result.Count; i++)
        {
            DownstreamRoute dr = _result[i].DownstreamRoute[0];
            dr.RateLimitOptions.EnableRateLimiting.ShouldBe(_rlo[i].EnableRateLimiting);
            dr.RateLimitOptions.ShouldBe(_rlo[i]);
            dr.DownstreamHttpVersion.ShouldBe(_version);
            dr.DownstreamHttpVersionPolicy.ShouldBe(_versionPolicy);
            dr.ServiceName.ShouldBe(_fileConfig.DynamicRoutes[i].ServiceName);
        }
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
        _rlo = [
            new() { EnableRateLimiting = false },
            new() { EnableRateLimiting = true },
        ];
        _rloCreator
            .SetupSequence(x => x.Create(It.IsAny<IRouteRateLimiting>(), It.IsAny<FileGlobalConfiguration>()))
            .Returns(_rlo[0])
            .Returns(_rlo[1]);
    }
}
