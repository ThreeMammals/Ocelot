using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;

namespace Ocelot.UnitTests.DownstreamRouteFinder;

public class DownstreamRouteCreatorTests : UnitTest
{
    private readonly DownstreamRouteCreator _creator;
    private readonly QoSOptions _qoSOptions;
    private readonly HttpHandlerOptions _handlerOptions;
    private readonly LoadBalancerOptions _loadBalancerOptions;
    private Response<Ocelot.DownstreamRouteFinder.DownstreamRouteHolder> _result;
    private string _upstreamHost;
    private string _upstreamUrlPath;
    private string _upstreamHttpMethod;
    private Dictionary<string, string> _upstreamHeaders;
    private IInternalConfiguration _configuration;
    private Response<Ocelot.DownstreamRouteFinder.DownstreamRouteHolder> _resultTwo;
    private readonly string _upstreamQuery;

    public DownstreamRouteCreatorTests()
    {
        _qoSOptions = new(new FileQoSOptions());
        _handlerOptions = new HttpHandlerOptionsBuilder().Build();
        _loadBalancerOptions = new LoadBalancerOptionsBuilder().WithType(nameof(NoLoadBalancer)).Build();
        _creator = new DownstreamRouteCreator();
        _upstreamQuery = string.Empty;
    }

    [Fact]
    public void Should_create_downstream_route()
    {
        // Arrange
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration);

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated();
    }

    [Fact]
    public void Should_create_downstream_route_with_rate_limit_options()
    {
        // Arrange
        var rateLimitOptions = new RateLimitOptionsBuilder()
            .WithEnableRateLimiting(true)
            .WithClientIdHeader("test")
            .Build();
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithServiceName("auth")
            .WithRateLimitOptions(rateLimitOptions)
            .Build();
        var route = new RouteBuilder()
            .WithDownstreamRoute(downstreamRoute)
            .Build();
        var routes = new List<Route> { route };
        var configuration = new InternalConfiguration(
            routes,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration);

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated();
        WithRateLimitOptions(rateLimitOptions);
    }

    [Fact]
    public void Should_cache_downstream_route()
    {
        // Arrange
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration, "/geoffisthebest/");

        // Act
        WhenICreate();
        GivenTheConfiguration(configuration, "/geoffisthebest/");
        WhenICreateAgain();

        // Assert
        _result.ShouldBe(_resultTwo);
    }

    [Fact]
    public void Should_not_cache_downstream_route()
    {
        // Arrange
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration, "/geoffistheworst/");

        // Act
        WhenICreate();
        GivenTheConfiguration(configuration, "/geoffisthebest/");
        WhenICreateAgain();

        // Assert
        _result.ShouldNotBe(_resultTwo);
    }

    [Fact]
    public void Should_create_downstream_route_with_no_path()
    {
        // Arrange
        var upstreamUrlPath = "/auth/";
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration, upstreamUrlPath);

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamPathIsForwardSlash();
    }

    [Fact]
    public void Should_create_downstream_route_with_only_first_segment_no_traling_slash()
    {
        // Arrange
        var upstreamUrlPath = "/auth";
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration, upstreamUrlPath);

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamPathIsForwardSlash();
    }

    [Fact]
    public void Should_create_downstream_route_with_segments_no_traling_slash()
    {
        // Arrange
        var upstreamUrlPath = "/auth/test";
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrHigher);
        GivenTheConfiguration(configuration, upstreamUrlPath);

        // Act
        WhenICreate();

        // Assert
        ThenThePathDoesNotHaveTrailingSlash();
    }

    [Fact]
    public void Should_create_downstream_route_and_remove_query_string()
    {
        // Arrange
        var upstreamUrlPath = "/auth/test?test=1&best=2";
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration, upstreamUrlPath);

        // Act
        WhenICreate();

        // Assert
        ThenTheQueryStringIsRemoved();
    }

    [Fact]
    public void Should_create_downstream_route_for_sticky_sessions()
    {
        // Arrange
        var loadBalancerOptions = new LoadBalancerOptionsBuilder().WithType(nameof(CookieStickySessions)).WithKey("boom").WithExpiryInMs(1).Build();
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration);

        // Act
        WhenICreate();

        // Assert
        ThenTheStickySessionLoadBalancerIsUsed(loadBalancerOptions);
    }

    [Fact]
    public void Should_create_downstream_route_with_qos()
    {
        // Arrange
        var qoSOptions = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(1)
            .WithTimeoutValue(1)
            .WithKey("/auth/test|GET")
            .Build();
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration);

        // Act
        WhenICreate();

        // Assert
        ThenTheQosOptionsAreSet(qoSOptions);
    }

    [Fact]
    public void Should_create_downstream_route_with_handler_options()
    {
        // Arrange
        var configuration = new InternalConfiguration(
            null,
            "doesnt matter",
            null,
            "doesnt matter",
            _loadBalancerOptions,
            "http",
            _qoSOptions,
            _handlerOptions,
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        GivenTheConfiguration(configuration);

        // Act
        WhenICreate();

        // Assert: Then The Handler Options Are Set
        _result.Data.Route.DownstreamRoute[0].HttpHandlerOptions.ShouldBe(_handlerOptions);
    }

    private void WithRateLimitOptions(RateLimitOptions expected)
    {
        _result.Data.Route.DownstreamRoute[0].EnableEndpointEndpointRateLimiting.ShouldBeTrue();
        _result.Data.Route.DownstreamRoute[0].RateLimitOptions.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
        _result.Data.Route.DownstreamRoute[0].RateLimitOptions.ClientIdHeader.ShouldBe(expected.ClientIdHeader);
    }

    private void ThenTheDownstreamRouteIsCreated()
    {
        _result.Data.Route.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
        _result.Data.Route.UpstreamHttpMethod[0].ShouldBe(HttpMethod.Get);
        _result.Data.Route.DownstreamRoute[0].ServiceName.ShouldBe("auth");
        _result.Data.Route.DownstreamRoute[0].LoadBalancerKey.ShouldBe("/auth/test|GET");
        _result.Data.Route.DownstreamRoute[0].UseServiceDiscovery.ShouldBeTrue();
        _result.Data.Route.DownstreamRoute[0].HttpHandlerOptions.ShouldNotBeNull();
        _result.Data.Route.DownstreamRoute[0].QosOptions.ShouldNotBeNull();
        _result.Data.Route.DownstreamRoute[0].DownstreamScheme.ShouldBe("http");
        _result.Data.Route.DownstreamRoute[0].LoadBalancerOptions.Type.ShouldBe(nameof(NoLoadBalancer));
        _result.Data.Route.DownstreamRoute[0].HttpHandlerOptions.ShouldBe(_handlerOptions);
        _result.Data.Route.DownstreamRoute[0].QosOptions.ShouldNotBeNull().Key.ShouldBe("/auth/test|GET");
        _result.Data.Route.UpstreamTemplatePattern.ShouldNotBeNull();
        _result.Data.Route.DownstreamRoute[0].UpstreamPathTemplate.ShouldNotBeNull();
    }

    private void ThenTheDownstreamPathIsForwardSlash()
    {
        _result.Data.Route.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe("/");
        _result.Data.Route.DownstreamRoute[0].ServiceName.ShouldBe("auth");
        _result.Data.Route.DownstreamRoute[0].LoadBalancerKey.ShouldBe("/auth/|GET");
    }

    private void ThenThePathDoesNotHaveTrailingSlash()
    {
        _result.Data.Route.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
        _result.Data.Route.DownstreamRoute[0].ServiceName.ShouldBe("auth");
        _result.Data.Route.DownstreamRoute[0].LoadBalancerKey.ShouldBe("/auth/test|GET");
    }

    private void ThenTheQueryStringIsRemoved()
    {
        _result.Data.Route.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
        _result.Data.Route.DownstreamRoute[0].ServiceName.ShouldBe("auth");
        _result.Data.Route.DownstreamRoute[0].LoadBalancerKey.ShouldBe("/auth/test|GET");
    }

    private void ThenTheStickySessionLoadBalancerIsUsed(LoadBalancerOptions expected)
    {
        _result.Data.Route.DownstreamRoute[0].LoadBalancerKey.ShouldBe($"{nameof(CookieStickySessions)}:boom");
        _result.Data.Route.DownstreamRoute[0].LoadBalancerOptions.Type.ShouldBe(nameof(CookieStickySessions));
        _result.Data.Route.DownstreamRoute[0].LoadBalancerOptions.ShouldBe(expected);
    }

    private void ThenTheQosOptionsAreSet(QoSOptions expected)
    {
        _result.Data.Route.DownstreamRoute[0].QosOptions.ShouldNotBeNull().Key.ShouldBe(expected.Key);
        _result.Data.Route.DownstreamRoute[0].QosOptions.UseQos.ShouldBeTrue();
    }

    private void GivenTheConfiguration(IInternalConfiguration config)
    {
        _upstreamHost = "doesnt matter";
        _upstreamUrlPath = "/auth/test";
        _upstreamHttpMethod = "GET";
        _upstreamHeaders = new Dictionary<string, string>();
        _configuration = config;
    }

    private void GivenTheConfiguration(IInternalConfiguration config, string upstreamUrlPath)
    {
        _upstreamHost = "doesnt matter";
        _upstreamUrlPath = upstreamUrlPath;
        _upstreamHttpMethod = "GET";
        _configuration = config;
    }

    private void WhenICreate()
    {
        _result = _creator.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _configuration, _upstreamHost, _upstreamHeaders);
    }

    private void WhenICreateAgain()
    {
        _resultTwo = _creator.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _configuration, _upstreamHost, _upstreamHeaders);
    }
}
