using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Responses;
using Ocelot.Values;
using System.Reflection;

namespace Ocelot.UnitTests.DownstreamRouteFinder;

public class DiscoveryDownstreamRouteFinderTests : UnitTest
{
    private readonly DiscoveryDownstreamRouteFinder _finder;
    private QoSOptions _qoSOptions;
    private readonly HttpHandlerOptions _handlerOptions;
    private LoadBalancerOptions _loadBalancerOptions;
    private Response<Ocelot.DownstreamRouteFinder.DownstreamRouteHolder> _result;
    private string _upstreamHost;
    private string _upstreamUrlPath;
    private string _upstreamHttpMethod;
    private Dictionary<string, string> _upstreamHeaders;
    private IInternalConfiguration _configuration;
    private Response<Ocelot.DownstreamRouteFinder.DownstreamRouteHolder> _resultTwo;
    private readonly string _upstreamQuery;
    private readonly Mock<IUpstreamHeaderTemplatePatternCreator> _upstreamHeaderTemplatePatternCreator = new();
    private readonly MetadataOptions _metadataOptions;
    private readonly RateLimitOptions _rateLimitOptions;

    public DiscoveryDownstreamRouteFinderTests()
    {
        _qoSOptions = new(new FileQoSOptions());
        _handlerOptions = new HttpHandlerOptionsBuilder().Build();
        _loadBalancerOptions = new(nameof(NoLoadBalancer), default, default);
        _metadataOptions = new MetadataOptions();
        _rateLimitOptions = new RateLimitOptions();
        _finder = new(new RouteKeyCreator(), _upstreamHeaderTemplatePatternCreator.Object);
        _upstreamQuery = string.Empty;
    }

    [Fact]
    public void Should_create_downstream_route()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated();
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "1229")]
    public void Should_create_downstream_route_with_rate_limit_options()
    {
        // Arrange
        var rateLimitOptions = new RateLimitOptions()
        {
            EnableRateLimiting = true,
            ClientIdHeader = "test",
        };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithServiceName("auth")
            .WithRateLimitOptions(rateLimitOptions)
            .WithLoadBalancerKey("|auth")
            .WithLoadBalancerOptions(_loadBalancerOptions)
            .WithQosOptions(_qoSOptions)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        var route = new Route(true, downstreamRoute); // create dynamic route
        GivenInternalConfiguration(route);
        GivenTheConfiguration();

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated(lbKey: downstreamRoute.LoadBalancerKey);

        // Assert: With RateLimitOptions
        var actual = _result.Data.Route.DownstreamRoute[0].RateLimitOptions;
        actual.EnableRateLimiting.ShouldBeTrue();
        actual.EnableRateLimiting.ShouldBe(rateLimitOptions.EnableRateLimiting);
        actual.ClientIdHeader.ShouldBe(rateLimitOptions.ClientIdHeader);
    }

    [Fact]
    public void Should_cache_downstream_route()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();
        _upstreamUrlPath = "/geoffisthebest/";

        // Act
        WhenICreate();
        GivenTheConfiguration();
        _upstreamUrlPath = "/geoffisthebest/";
        WhenICreateAgain();

        // Assert
        _result.ShouldBe(_resultTwo);
    }

    [Fact]
    public void Should_not_cache_downstream_route()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();
        _upstreamUrlPath = "/geoffistheworst/";

        // Act
        WhenICreate();
        GivenTheConfiguration();
        _upstreamUrlPath = "/geoffisthebest/";
        WhenICreateAgain();

        // Assert
        _result.ShouldNotBe(_resultTwo);
    }

    [Fact]
    public void Should_create_downstream_route_with_no_path()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();
        _upstreamUrlPath = "/auth/";

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamPathIsForwardSlash();
    }

    [Fact]
    public void Should_create_downstream_route_with_only_first_segment_no_traling_slash()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();
        _upstreamUrlPath = "/auth";

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamPathIsForwardSlash();
    }

    [Fact]
    public void Should_create_downstream_route_with_segments_no_traling_slash()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();
        _upstreamUrlPath = "/auth/test";

        // Act
        WhenICreate();

        // Assert: Then the path does not have trailing slash
        var actual = _result.Data.Route.DownstreamRoute[0];
        actual.DownstreamPathTemplate.Value.ShouldBe("/test");
        actual.ServiceName.ShouldBe("auth");
        actual.ServiceNamespace.ShouldBeEmpty();
        actual.LoadBalancerKey.ShouldBe(".auth");
    }

    [Fact]
    [Trait("Feat", "351")]
    [Trait("PR", "2324")] // This PR resolves the issue of forwarding the query string to the downstream when service discovery (dynamic routing), fixing a bug in the QoS Key construction for caching within the ResiliencePipelineRegistry<T>. It now reuses the load balancing key to address the problem.
    public void Should_create_downstream_route_and_forward_query_string()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();
        const string queryString = "?test=1&best=2";
        _upstreamUrlPath = "/auth/test" + queryString;

        // Act
        WhenICreate();

        // Assert: Then the query string is removed
        var actual = _result.Data.Route.DownstreamRoute[0];
        actual.DownstreamPathTemplate.Value.ShouldContain(queryString); // !!!
        actual.DownstreamPathTemplate.Value.ShouldBe("/test?test=1&best=2");
        actual.ServiceName.ShouldBe("auth");
        actual.ServiceNamespace.ShouldBeEmpty();
        actual.LoadBalancerKey.ShouldBe(".auth");
    }

    [Fact]
    public void Should_create_downstream_route_for_sticky_sessions()
    {
        // Arrange
        _loadBalancerOptions = new LoadBalancerOptions(nameof(CookieStickySessions), "boom", 1);
        GivenInternalConfiguration();
        GivenTheConfiguration();

        // Act
        WhenICreate();

        // Assert
        var actual = _result.Data.Route.DownstreamRoute[0];
        actual.LoadBalancerKey.ShouldBe("CookieStickySessions:boom");
        actual.LoadBalancerOptions.Type.ShouldBe("CookieStickySessions");
        actual.LoadBalancerOptions.ShouldBe(_loadBalancerOptions);
    }

    [Fact]
    public void Should_create_downstream_route_with_qos()
    {
        // Arrange
        _qoSOptions = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(1)
            .WithTimeoutValue(1)
            .Build();
        GivenInternalConfiguration();
        GivenTheConfiguration();

        // Act
        WhenICreate();

        // Assert: Then the Qos options are set
        var actual = _result.Data.Route.DownstreamRoute[0];
        actual.QosOptions.ShouldNotBeNull();
        actual.QosOptions.UseQos.ShouldBeTrue();
    }

    [Fact]
    public void Should_create_downstream_route_with_handler_options()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();

        // Act
        WhenICreate();

        // Assert: Then The Handler Options Are Set
        _result.Data.Route.DownstreamRoute[0].HttpHandlerOptions.ShouldBe(_handlerOptions);
    }

    [Theory]
    [Trait("PR", "2324")]
    [InlineData("/service1", "service1", "")]
    [InlineData("/service2/", "service2", "")]
    [InlineData("/service3/bla", "service3", "")]
    [InlineData("/namespace1.service1", "service1", "namespace1")]
    [InlineData("/namespace2.service2/", "service2", "namespace2")]
    [InlineData("/namespace3.service3/bla-bla", "service3", "namespace3")]
    [InlineData("/namespace4.service.4/ha-a", "service.4", "namespace4")]
    [InlineData("/name.space5.service5/ha-ha", "space5.service5", "name")]
    public void GetServiceName(string urlPath, string expected, string expectedNamespace)
    {
        var method = _finder.GetType().GetMethod(nameof(GetServiceName), BindingFlags.Instance | BindingFlags.NonPublic);
        object[] parameters = [urlPath, null];

        // Act
        string actual = (string)method.Invoke(_finder, parameters);
        string actualNamespace = (string)parameters[1];

        Assert.Equal(expected, actual);
        Assert.Equal(expectedNamespace, actualNamespace);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void Should_create_downstream_route_with_load_balancer_options()
    {
        // Arrange
        var lbOptions = new LoadBalancerOptions("testBalancer", "testKey", 3);
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithServiceName("auth")
            .WithLoadBalancerOptions(lbOptions)
            .WithLoadBalancerKey("|auth")
            .WithMetadata(_metadataOptions)
            .WithRateLimitOptions(_rateLimitOptions)
            .WithQosOptions(_qoSOptions)
            .WithDownstreamScheme("http")
            .Build();
        var route = new Route(true, downstreamRoute); // create dynamic route
        GivenInternalConfiguration(route);
        GivenTheConfiguration();

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated(lbType: "testBalancer", lbKey: "|auth");
        var downstream = _result.Data.Route.DownstreamRoute[0];
        downstream.LoadBalancerOptions.ShouldNotBeNull();
        downstream.LoadBalancerOptions.Type.ShouldBe("testBalancer");
        downstream.LoadBalancerOptions.Key.ShouldBe("testKey");
        downstream.LoadBalancerOptions.ExpiryInMs.ShouldBe(3);
    }

    [Theory]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    [InlineData(false)]
    [InlineData(true)]
    public void ShouldFindFirstOrDefaultDownstreamRoute_WithOrWithoutServiceNamespace(bool hasNamespace)
    {
        // Arrange
        var lbOptions = new LoadBalancerOptions("testBalancer", "testKey", 3);
        var dRoute1 = new DownstreamRouteBuilder()
            .WithServiceName("service1")
            .Build();
        var dRoute2 = new DownstreamRouteBuilder()
            .WithServiceName("service2")
            .WithServiceNamespace(hasNamespace ? "namespace2" : string.Empty)
            .WithLoadBalancerKey("namespace2-service2")
            .WithLoadBalancerOptions(lbOptions)
            .WithQosOptions(_qoSOptions)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        var route = new Route(true)
        {
            DownstreamRoute = [dRoute1, dRoute2],
        };
        GivenInternalConfiguration(route, 1);
        GivenTheConfiguration();
        _upstreamUrlPath = hasNamespace
            ? $"/{dRoute2.ServiceNamespace}.{dRoute2.ServiceName}/test"
            : $"/{dRoute2.ServiceName}/test";

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated("service2", hasNamespace ? "namespace2" : "", "testBalancer", "namespace2-service2");
        var downstream = _result.Data.Route.DownstreamRoute[0];
        downstream.LoadBalancerOptions.ShouldNotBeNull();
        downstream.LoadBalancerOptions.Type.ShouldBe("testBalancer");
        downstream.LoadBalancerOptions.Key.ShouldBe("testKey");
        downstream.LoadBalancerOptions.ExpiryInMs.ShouldBe(3);
    }

    private void ThenTheDownstreamRouteIsCreated(string serviceName = null, string serviceNamespace = null, string lbType = null, string lbKey = null)
    {
        _result.Data.Route.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
        _result.Data.Route.UpstreamHttpMethod.ShouldContain(HttpMethod.Get);
        _result.Data.Route.DownstreamRoute[0].ServiceName.ShouldBe(serviceName ?? "auth");
        _result.Data.Route.DownstreamRoute[0].ServiceNamespace.ShouldBe(serviceNamespace ?? string.Empty);
        _result.Data.Route.DownstreamRoute[0].LoadBalancerKey.ShouldBe(lbKey ?? ".auth");
        _result.Data.Route.DownstreamRoute[0].UseServiceDiscovery.ShouldBeTrue();
        _result.Data.Route.DownstreamRoute[0].HttpHandlerOptions.ShouldNotBeNull();
        _result.Data.Route.DownstreamRoute[0].QosOptions.ShouldNotBeNull();
        _result.Data.Route.DownstreamRoute[0].DownstreamScheme.ShouldBe("http");
        _result.Data.Route.DownstreamRoute[0].LoadBalancerOptions.Type.ShouldBe(lbType ?? nameof(NoLoadBalancer));
        _result.Data.Route.DownstreamRoute[0].HttpHandlerOptions.ShouldBe(_handlerOptions);
        _result.Data.Route.DownstreamRoute[0].QosOptions.ShouldNotBeNull();
        _result.Data.Route.UpstreamTemplatePattern.ShouldNotBeNull();
        _result.Data.Route.DownstreamRoute[0].UpstreamPathTemplate.ShouldNotBeNull();
        var kv = _upstreamHeaders.First();
        _result.Data.Route.UpstreamHeaderTemplates.ShouldNotBeNull()
            .FirstOrDefault(x => x.Key == kv.Key).Value.Template.ShouldBe(kv.Value);
        _result.Data.Route.DownstreamRoute[0].UpstreamHeaders.ShouldNotBeNull()
            .FirstOrDefault(x => x.Key == kv.Key).Value.Template.ShouldBe(kv.Value);
    }

    private void ThenTheDownstreamPathIsForwardSlash()
    {
        _result.Data.Route.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe("/");
        _result.Data.Route.DownstreamRoute[0].ServiceName.ShouldBe("auth");
        _result.Data.Route.DownstreamRoute[0].ServiceNamespace.ShouldBeEmpty();
        _result.Data.Route.DownstreamRoute[0].LoadBalancerKey.ShouldBe(".auth");
    }

    private void GivenTheConfiguration()
    {
        _upstreamHost = "doesnt matter";
        _upstreamUrlPath = "/auth/test";
        _upstreamHttpMethod = "GET";
        _upstreamHeaders = new()
        {
            { "testHeader", "testHeaderValue" },
        };
        var kv = _upstreamHeaders.First();
        _upstreamHeaderTemplatePatternCreator.Setup(x => x.Create(It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
            .Returns(new Dictionary<string, UpstreamHeaderTemplate>()
            {
                { kv.Key, new(kv.Value, kv.Value) },
            });
    }

    private void GivenInternalConfiguration(Route route = null, int index = 0)
    {
        var dr = route?.DownstreamRoute[index];
        _configuration = new InternalConfiguration(
            route is null ? null : new() { route },
            "/AdminPath",
            null,
            "requestID",
            dr?.LoadBalancerOptions ?? _loadBalancerOptions,
            (dr?.DownstreamScheme).IfEmpty(Uri.UriSchemeHttp),
            dr?.QosOptions ?? _qoSOptions,
            dr?.HttpHandlerOptions ?? _handlerOptions,
            dr?.DownstreamHttpVersion ?? new Version("1.1"),
            dr?.DownstreamHttpVersionPolicy ?? HttpVersionPolicy.RequestVersionOrLower,
            dr?.MetadataOptions ?? _metadataOptions,
            dr?.RateLimitOptions ?? _rateLimitOptions,
            dr?.Timeout ?? 111);
    }

    private void WhenICreate()
    {
        _result = _finder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _configuration, _upstreamHost, _upstreamHeaders);
    }

    private void WhenICreateAgain()
    {
        _resultTwo = _finder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _configuration, _upstreamHost, _upstreamHeaders);
    }
}
