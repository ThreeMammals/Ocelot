﻿using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.UnitTests.Configuration;

public class RoutesCreatorTests : UnitTest
{
    private readonly RoutesCreator _creator;
    private readonly Mock<IClaimsToThingCreator> _cthCreator;
    private readonly Mock<IAuthenticationOptionsCreator> _aoCreator;
    private readonly Mock<IUpstreamTemplatePatternCreator> _utpCreator;
    private readonly Mock<IUpstreamHeaderTemplatePatternCreator> _uhtpCreator;
    private readonly Mock<IRequestIdKeyCreator> _ridkCreator;
    private readonly Mock<IQoSOptionsCreator> _qosoCreator;
    private readonly Mock<IRouteOptionsCreator> _rroCreator;
    private readonly Mock<IRateLimitOptionsCreator> _rloCreator;
    private readonly Mock<ICacheOptionsCreator> _coCreator;
    private readonly Mock<IHttpHandlerOptionsCreator> _hhoCreator;
    private readonly Mock<IHeaderFindAndReplaceCreator> _hfarCreator;
    private readonly Mock<IDownstreamAddressesCreator> _daCreator;
    private readonly Mock<ILoadBalancerOptionsCreator> _lboCreator;
    private readonly Mock<IRouteKeyCreator> _rrkCreator;
    private readonly Mock<ISecurityOptionsCreator> _soCreator;
    private readonly Mock<IVersionCreator> _versionCreator;
    private readonly Mock<IVersionPolicyCreator> _versionPolicyCreator;
    private readonly Mock<IMetadataCreator> _metadataCreator;
    private FileConfiguration _fileConfig;
    private RouteOptions _rro;
    private string _requestId;
    private string _rrk;
    private UpstreamPathTemplate _upt;
    private AuthenticationOptions _ao;
    private List<ClaimToThing> _ctt;
    private QoSOptions _qoso;
    private RateLimitOptions _rlo;
    private CacheOptions _cacheOptions;
    private HttpHandlerOptions _hho;
    private HeaderTransformations _ht;
    private List<DownstreamHostAndPort> _dhp;
    private LoadBalancerOptions _lbo;
    private IReadOnlyList<Route> _result;
    private Version _expectedVersion;
    private HttpVersionPolicy _expectedVersionPolicy;
    private Dictionary<string, UpstreamHeaderTemplate> _uht;
    private Dictionary<string, string> _expectedMetadata;

    public RoutesCreatorTests()
    {
        _cthCreator = new Mock<IClaimsToThingCreator>();
        _aoCreator = new Mock<IAuthenticationOptionsCreator>();
        _utpCreator = new Mock<IUpstreamTemplatePatternCreator>();
        _ridkCreator = new Mock<IRequestIdKeyCreator>();
        _qosoCreator = new Mock<IQoSOptionsCreator>();
        _rroCreator = new Mock<IRouteOptionsCreator>();
        _rloCreator = new Mock<IRateLimitOptionsCreator>();
        _coCreator = new Mock<ICacheOptionsCreator>();
        _hhoCreator = new Mock<IHttpHandlerOptionsCreator>();
        _hfarCreator = new Mock<IHeaderFindAndReplaceCreator>();
        _daCreator = new Mock<IDownstreamAddressesCreator>();
        _lboCreator = new Mock<ILoadBalancerOptionsCreator>();
        _rrkCreator = new Mock<IRouteKeyCreator>();
        _soCreator = new Mock<ISecurityOptionsCreator>();
        _versionCreator = new Mock<IVersionCreator>();
        _versionPolicyCreator = new Mock<IVersionPolicyCreator>();
        _uhtpCreator = new Mock<IUpstreamHeaderTemplatePatternCreator>();
        _metadataCreator = new Mock<IMetadataCreator>();

        _creator = new RoutesCreator(
            _cthCreator.Object,
            _aoCreator.Object,
            _utpCreator.Object,
            _ridkCreator.Object,
            _qosoCreator.Object,
            _rroCreator.Object,
            _rloCreator.Object,
            _coCreator.Object,
            _hhoCreator.Object,
            _hfarCreator.Object,
            _daCreator.Object,
            _lboCreator.Object,
            _rrkCreator.Object,
            _soCreator.Object,
            _versionCreator.Object,
            _versionPolicyCreator.Object,
            _uhtpCreator.Object,
            _metadataCreator.Object);
    }

    [Fact]
    public void Should_return_nothing()
    {
        // Arrange
        _fileConfig = new FileConfiguration();

        // Act
        _result = _creator.Create(_fileConfig);

        // Assert
        _result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_return_routes()
    {
        // Arrange
        _fileConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    ServiceName = "dave",
                    DangerousAcceptAnyServerCertificateValidator = true,
                    AddClaimsToRequest = new Dictionary<string, string>
                    {
                        { "a","b" },
                    },
                    AddHeadersToRequest = new Dictionary<string, string>
                    {
                        { "c","d" },
                    },
                    AddQueriesToRequest = new Dictionary<string, string>
                    {
                        { "e","f" },
                    },
                    UpstreamHttpMethod = new List<string> { "GET", "POST" },
                    Metadata = new Dictionary<string, string>
                    {
                        ["foo"] = "bar",
                    },
                },
                new()
                {
                    ServiceName = "wave",
                    DangerousAcceptAnyServerCertificateValidator = false,
                    AddClaimsToRequest = new Dictionary<string, string>
                    {
                        { "g","h" },
                    },
                    AddHeadersToRequest = new Dictionary<string, string>
                    {
                        { "i","j" },
                    },
                    AddQueriesToRequest = new Dictionary<string, string>
                    {
                        { "k","l" },
                    },
                    UpstreamHttpMethod = new List<string> { "PUT", "DELETE" },
                    Metadata = new Dictionary<string, string>
                    {
                        ["foo"] = "baz",
                    },
                },
            },
        };
        GivenTheDependenciesAreSetUpCorrectly();

        // Act
        _result = _creator.Create(_fileConfig);

        // Assert
        ThenTheDependenciesAreCalledCorrectly();
        ThenTheRoutesAreCreated();
    }

    #region PR 2073

    [Fact]
    [Trait("PR", "2073")] // https://github.com/ThreeMammals/Ocelot/pull/2073
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    [Trait("Feat", "1869")] // https://github.com/ThreeMammals/Ocelot/issues/1869
    public void CreateTimeout_HasRouteTimeout_ShouldCreateFromRoute()
    {
        // Arrange
        var route = new FileRoute { Timeout = 11 };
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
        var route = new FileRoute();
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
        var route = new FileRoute();
        var global = new FileGlobalConfiguration();

        // Act
        var timeout = _creator.CreateTimeout(route, global);

        // Assert
        Assert.Null(route.Timeout);
        Assert.Null(global.Timeout);
        Assert.Equal(DownstreamRoute.DefTimeout, timeout);
    }
    #endregion

    private void ThenTheDependenciesAreCalledCorrectly()
    {
        ThenTheDepsAreCalledFor(_fileConfig.Routes[0], _fileConfig.GlobalConfiguration);
        ThenTheDepsAreCalledFor(_fileConfig.Routes[1], _fileConfig.GlobalConfiguration);
    }

    private void GivenTheDependenciesAreSetUpCorrectly()
    {
        _expectedVersion = new Version("1.1");
        _expectedVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        _rro = new RouteOptions(false, false, false, false, false);
        _requestId = "testy";
        _rrk = "besty";
        _upt = new UpstreamPathTemplateBuilder().Build();
        _ao = new AuthenticationOptionsBuilder().Build();
        _ctt = new List<ClaimToThing>();
        _qoso = new QoSOptionsBuilder().Build();
        _rlo = new RateLimitOptionsBuilder().Build();

        _cacheOptions = new CacheOptions(0, "vesty", null, false);
        _hho = new HttpHandlerOptionsBuilder().Build();
        _ht = new HeaderTransformations(new List<HeaderFindAndReplace>(), new List<HeaderFindAndReplace>(), new List<AddHeader>(), new List<AddHeader>());
        _dhp = new List<DownstreamHostAndPort>();
        _lbo = new LoadBalancerOptionsBuilder().Build();
        _uht = new Dictionary<string, UpstreamHeaderTemplate>();
        _expectedMetadata = new Dictionary<string, string>()
        {
            ["foo"] = "bar",
        };

        _rroCreator.Setup(x => x.Create(It.IsAny<FileRoute>())).Returns(_rro);
        _ridkCreator.Setup(x => x.Create(It.IsAny<FileRoute>(), It.IsAny<FileGlobalConfiguration>())).Returns(_requestId);
        _rrkCreator.Setup(x => x.Create(It.IsAny<FileRoute>())).Returns(_rrk);
        _utpCreator.Setup(x => x.Create(It.IsAny<IRoute>())).Returns(_upt);
        _aoCreator.Setup(x => x.Create(It.IsAny<FileRoute>())).Returns(_ao);
        _cthCreator.Setup(x => x.Create(It.IsAny<Dictionary<string, string>>())).Returns(_ctt);
        _qosoCreator.Setup(x => x.Create(It.IsAny<FileRoute>(), It.IsAny<FileGlobalConfiguration>())).Returns(_qoso);
        _rloCreator.Setup(x => x.Create(It.IsAny<FileRateLimitRule>(), It.IsAny<FileGlobalConfiguration>())).Returns(_rlo);
        _coCreator.Setup(x => x.Create(It.IsAny<FileCacheOptions>(), It.IsAny<FileGlobalConfiguration>(), It.IsAny<string>(), It.IsAny<IList<string>>())).Returns(_cacheOptions);
        _hhoCreator.Setup(x => x.Create(It.IsAny<FileHttpHandlerOptions>())).Returns(_hho);
        _hfarCreator.Setup(x => x.Create(It.IsAny<FileRoute>())).Returns(_ht);
        _hfarCreator.Setup(x => x.Create(It.IsAny<FileRoute>(), It.IsAny<FileGlobalConfiguration>())).Returns(_ht);
        _daCreator.Setup(x => x.Create(It.IsAny<FileRoute>())).Returns(_dhp);
        _lboCreator.Setup(x => x.Create(It.IsAny<FileLoadBalancerOptions>())).Returns(_lbo);
        _versionCreator.Setup(x => x.Create(It.IsAny<string>())).Returns(_expectedVersion);
        _versionPolicyCreator.Setup(x => x.Create(It.IsAny<string>())).Returns(_expectedVersionPolicy);
        _uhtpCreator.Setup(x => x.Create(It.IsAny<FileRoute>())).Returns(_uht);
        _metadataCreator.Setup(x => x.Create(It.IsAny<IDictionary<string, string>>(), It.IsAny<FileGlobalConfiguration>())).Returns(new MetadataOptions(new FileMetadataOptions
        {
            Metadata = _expectedMetadata,
        }));
    }

    private void ThenTheRoutesAreCreated()
    {
        _result.Count.ShouldBe(2);
        ThenTheRouteIsSet(_fileConfig.Routes[0], 0);
        ThenTheRouteIsSet(_fileConfig.Routes[1], 1);
    }

    private void ThenTheRouteIsSet(FileRoute expected, int routeIndex)
    {
        _result[routeIndex].DownstreamRoute[0].DownstreamHttpVersion.ShouldBe(_expectedVersion);
        _result[routeIndex].DownstreamRoute[0].DownstreamHttpVersionPolicy.ShouldBe(_expectedVersionPolicy);
        _result[routeIndex].DownstreamRoute[0].IsAuthenticated.ShouldBe(_rro.IsAuthenticated);
        _result[routeIndex].DownstreamRoute[0].IsAuthorized.ShouldBe(_rro.IsAuthorized);
        _result[routeIndex].DownstreamRoute[0].IsCached.ShouldBe(_rro.IsCached);
        _result[routeIndex].DownstreamRoute[0].EnableEndpointEndpointRateLimiting.ShouldBe(_rro.EnableRateLimiting);
        _result[routeIndex].DownstreamRoute[0].RequestIdKey.ShouldBe(_requestId);
        _result[routeIndex].DownstreamRoute[0].LoadBalancerKey.ShouldBe(_rrk);
        _result[routeIndex].DownstreamRoute[0].UpstreamPathTemplate.ShouldBe(_upt);
        _result[routeIndex].DownstreamRoute[0].AuthenticationOptions.ShouldBe(_ao);
        _result[routeIndex].DownstreamRoute[0].ClaimsToHeaders.ShouldBe(_ctt);
        _result[routeIndex].DownstreamRoute[0].ClaimsToQueries.ShouldBe(_ctt);
        _result[routeIndex].DownstreamRoute[0].ClaimsToClaims.ShouldBe(_ctt);
        _result[routeIndex].DownstreamRoute[0].QosOptions.ShouldBe(_qoso);
        _result[routeIndex].DownstreamRoute[0].RateLimitOptions.ShouldBe(_rlo);
        _result[routeIndex].DownstreamRoute[0].CacheOptions.Region.ShouldBe(_cacheOptions.Region);
        _result[routeIndex].DownstreamRoute[0].CacheOptions.TtlSeconds.ShouldBe(0);
        _result[routeIndex].DownstreamRoute[0].HttpHandlerOptions.ShouldBe(_hho);
        _result[routeIndex].DownstreamRoute[0].UpstreamHeadersFindAndReplace.ShouldBe(_ht.Upstream);
        _result[routeIndex].DownstreamRoute[0].DownstreamHeadersFindAndReplace.ShouldBe(_ht.Downstream);
        _result[routeIndex].DownstreamRoute[0].AddHeadersToUpstream.ShouldBe(_ht.AddHeadersToUpstream);
        _result[routeIndex].DownstreamRoute[0].AddHeadersToDownstream.ShouldBe(_ht.AddHeadersToDownstream);
        _result[routeIndex].DownstreamRoute[0].DownstreamAddresses.ShouldBe(_dhp);
        _result[routeIndex].DownstreamRoute[0].LoadBalancerOptions.ShouldBe(_lbo);
        _result[routeIndex].DownstreamRoute[0].UseServiceDiscovery.ShouldBe(_rro.UseServiceDiscovery);
        _result[routeIndex].DownstreamRoute[0].DangerousAcceptAnyServerCertificateValidator.ShouldBe(expected.DangerousAcceptAnyServerCertificateValidator);
        _result[routeIndex].DownstreamRoute[0].DelegatingHandlers.ShouldBe(expected.DelegatingHandlers);
        _result[routeIndex].DownstreamRoute[0].ServiceName.ShouldBe(expected.ServiceName);
        _result[routeIndex].DownstreamRoute[0].DownstreamScheme.ShouldBe(expected.DownstreamScheme);
        _result[routeIndex].DownstreamRoute[0].RouteClaimsRequirement.ShouldBe(expected.RouteClaimsRequirement);
        _result[routeIndex].DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe(expected.DownstreamPathTemplate);
        _result[routeIndex].DownstreamRoute[0].Key.ShouldBe(expected.Key);
        _result[routeIndex].DownstreamRoute[0].MetadataOptions.Metadata.ShouldBe(_expectedMetadata);
        _result[routeIndex].UpstreamHttpMethod
            .Select(x => x.Method)
            .ToList()
            .ShouldContain(x => x == expected.UpstreamHttpMethod[0]);
        _result[routeIndex].UpstreamHttpMethod
            .Select(x => x.Method)
            .ToList()
            .ShouldContain(x => x == expected.UpstreamHttpMethod[1]);
        _result[routeIndex].UpstreamHost.ShouldBe(expected.UpstreamHost);
        _result[routeIndex].DownstreamRoute.Count.ShouldBe(1);
        _result[routeIndex].UpstreamTemplatePattern.ShouldBe(_upt);
        _result[routeIndex].UpstreamHeaderTemplates.ShouldBe(_uht);
    }

    private void ThenTheDepsAreCalledFor(FileRoute fileRoute, FileGlobalConfiguration globalConfig)
    {
        _rroCreator.Verify(x => x.Create(fileRoute), Times.Once);
        _ridkCreator.Verify(x => x.Create(fileRoute, globalConfig), Times.Once);
        _rrkCreator.Verify(x => x.Create(fileRoute), Times.Once);
        _utpCreator.Verify(x => x.Create(fileRoute), Times.Exactly(2));
        _aoCreator.Verify(x => x.Create(fileRoute), Times.Once);
        _cthCreator.Verify(x => x.Create(fileRoute.AddHeadersToRequest), Times.Once);
        _cthCreator.Verify(x => x.Create(fileRoute.AddClaimsToRequest), Times.Once);
        _cthCreator.Verify(x => x.Create(fileRoute.AddQueriesToRequest), Times.Once);
        _qosoCreator.Verify(x => x.Create(fileRoute, globalConfig));
        _rloCreator.Verify(x => x.Create(fileRoute.RateLimitOptions, globalConfig), Times.Once);
        _coCreator.Verify(x => x.Create(fileRoute.FileCacheOptions, globalConfig, fileRoute.UpstreamPathTemplate, fileRoute.UpstreamHttpMethod), Times.Once);
        _hhoCreator.Verify(x => x.Create(fileRoute.HttpHandlerOptions), Times.Once);
        _hfarCreator.Verify(x => x.Create(fileRoute), Times.Never);
        _hfarCreator.Verify(x => x.Create(fileRoute, globalConfig), Times.Once);
        _daCreator.Verify(x => x.Create(fileRoute), Times.Once);
        _lboCreator.Verify(x => x.Create(fileRoute.LoadBalancerOptions), Times.Once);
        _soCreator.Verify(x => x.Create(fileRoute.SecurityOptions, globalConfig), Times.Once);
        _metadataCreator.Verify(x => x.Create(fileRoute.Metadata, globalConfig), Times.Once);
    }
}
