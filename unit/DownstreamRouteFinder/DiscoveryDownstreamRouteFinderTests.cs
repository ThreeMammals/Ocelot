using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.Values;
using System.Reflection;
using Route = Ocelot.Configuration.Route;

namespace Ocelot.UnitTests.DownstreamRouteFinder;

public class DiscoveryDownstreamRouteFinderTests : UnitTest
{
    private DiscoveryDownstreamRouteFinder _finder;
    private QoSOptions _qoSOptions;
    private LoadBalancerOptions _loadBalancerOptions;
    private Response<Route> _result;
    private string _upstreamHost;
    private string _upstreamUrlPath;
    private string _upstreamHttpMethod;
    private IHeaderDictionary _upstreamHeaders;
    private IInternalConfiguration _configuration;
    private Response<Route> _resultTwo;
    private readonly string _upstreamQuery;
    private readonly Mock<IUpstreamHeaderTemplatePatternCreator> _headerTemplatePatternCreator = new();
    private readonly Mock<IDynamicRouting> _dynamicRouting = new();
    private readonly HttpHandlerOptions _handlerOptions;
    private readonly MetadataOptions _metadataOptions;
    private readonly RateLimitOptions _rateLimitOptions;

    public DiscoveryDownstreamRouteFinderTests()
    {
        _qoSOptions = new(new FileQoSOptions());
        _handlerOptions = new();
        _loadBalancerOptions = new(nameof(NoLoadBalancer), default, default);
        _metadataOptions = new MetadataOptions();
        _rateLimitOptions = new RateLimitOptions();
        _finder = new(new RouteKeyCreator(), _headerTemplatePatternCreator.Object, _dynamicRouting.Object);
        _dynamicRouting.Setup(x => x.GetDownstreamPath())
            .Returns(() => DownstreamPath(_upstreamUrlPath));
        _upstreamQuery = string.Empty;
    }

    private static string DownstreamPath(string urlPath)
    {
        return urlPath.IsEmpty() ? "/test"
            : '/' + string.Join('/', urlPath.TrimStart('/').Split('/').Skip(1));
    }

    private void GivenDynamicRouting(string upstreamUrlPath, string serviceName, string serviceNamespace = null)
    {
        _upstreamUrlPath = upstreamUrlPath;
        _dynamicRouting.Setup(x => x.GetServiceName(upstreamUrlPath))
            .Returns(new ServiceInfo(serviceName, serviceNamespace ?? string.Empty));
    }

    [Fact]
    public void Should_create_downstream_route()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();
        GivenDynamicRouting("/auth/test", "auth");

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
            .WithHttpHandlerOptions(_handlerOptions)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        var route = new Route(true, downstreamRoute); // create dynamic route
        GivenInternalConfiguration(route);
        GivenTheConfiguration();
        GivenDynamicRouting(null, downstreamRoute.ServiceName);

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated(lbKey: downstreamRoute.LoadBalancerKey);

        // Assert: With RateLimitOptions
        var actual = _result.Data.DownstreamRoute[0].RateLimitOptions;
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
        _upstreamUrlPath = "/GeoffIsTheBest/";

        // Act
        WhenICreate();
        GivenTheConfiguration();
        _upstreamUrlPath = "/GeoffIsTheBest/";
        WhenICreateAgain();

        // Assert
        Assert.NotSame(_resultTwo, _result);
        Assert.Same(_resultTwo.Data, _result.Data); // same Route instances
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
        GivenDynamicRouting("/auth/", "auth");

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
        GivenDynamicRouting("/auth", "auth");

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
        GivenDynamicRouting("/auth/test", "auth");

        // Act
        WhenICreate();

        // Assert: Then the path does not have trailing slash
        var actual = _result.Data.DownstreamRoute[0];
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
        GivenDynamicRouting("/auth/test" + queryString, "auth");

        // Act
        WhenICreate();

        // Assert: Then the query string is removed
        var actual = _result.Data.DownstreamRoute[0];
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
        var actual = _result.Data.DownstreamRoute[0];
        actual.LoadBalancerKey.ShouldBe("CookieStickySessions:boom");
        actual.LoadBalancerOptions.Type.ShouldBe("CookieStickySessions");
        actual.LoadBalancerOptions.ShouldBe(_loadBalancerOptions);
    }

    [Fact]
    public void Should_create_downstream_route_with_qos()
    {
        // Arrange
        _qoSOptions = new QoSOptions(1)
        {
            MinimumThroughput = 1,
        };
        GivenInternalConfiguration();
        GivenTheConfiguration();

        // Act
        WhenICreate();

        // Assert: Then the Qos options are set
        var actual = _result.Data.DownstreamRoute[0];
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
        _result.Data.DownstreamRoute[0].HttpHandlerOptions.ShouldBe(_handlerOptions);
    }

    private MethodInfo GetServiceName() => _finder.GetType()
        .GetMethod(nameof(GetServiceName), BindingFlags.Instance | BindingFlags.NonPublic, [typeof(string)]);
    protected ServiceInfo GetServiceName(string upstreamUrlPath)
        => (ServiceInfo)GetServiceName().Invoke(_finder, [upstreamUrlPath]);

    [Fact]
    [Trait("PR", "1198")]
    public void GetServiceName_ForwardedParamsToDependantService()
    {
        // Arrange
        string actualParam = null;
        _dynamicRouting.Setup(x => x.GetServiceName(It.IsAny<string>()))
            .Callback<string>(upstreamUrlPath => actualParam = upstreamUrlPath)
            .Returns(new ServiceInfo(TestName(), GetType().Name));

        // Act
        var path = UrlPathDynamicRouting.UpstreamPath(serviceNamespace: GetType().Name);
        var actual = GetServiceName(path);

        Assert.Same(path, actualParam);
        _dynamicRouting.Verify(x => x.GetServiceName(It.IsAny<string>()), Times.Once);

        Assert.Equal(TestName(), actual.Name);
        Assert.Equal(nameof(DiscoveryDownstreamRouteFinderTests), actual.Namespace);
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
            .WithHttpHandlerOptions(_handlerOptions)
            .WithDownstreamScheme("http")
            .Build();
        var route = new Route(true, downstreamRoute); // create dynamic route
        GivenInternalConfiguration(route);
        GivenTheConfiguration();
        GivenDynamicRouting(null, downstreamRoute.ServiceName);

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated(lbType: "testBalancer", lbKey: "|auth");
        var downstream = _result.Data.DownstreamRoute[0];
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
            .WithHttpHandlerOptions(_handlerOptions)
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
        GivenDynamicRouting(_upstreamUrlPath, dRoute2.ServiceName, dRoute2.ServiceNamespace);

        // Act
        WhenICreate();

        // Assert
        ThenTheDownstreamRouteIsCreated("service2", hasNamespace ? "namespace2" : "", "testBalancer", "namespace2-service2");
        var downstream = _result.Data.DownstreamRoute[0];
        downstream.LoadBalancerOptions.ShouldNotBeNull();
        downstream.LoadBalancerOptions.Type.ShouldBe("testBalancer");
        downstream.LoadBalancerOptions.Key.ShouldBe("testKey");
        downstream.LoadBalancerOptions.ExpiryInMs.ShouldBe(3);
    }

    [Fact]
    [Trait("Bug", "2352")] // https://github.com/ThreeMammals/Ocelot/issues/2352
    [Trait("PR", "2427")] // https://github.com/ThreeMammals/Ocelot/pull/2427
    public void Should_not_create_upstream_header_templates_from_request_headers()
    {
        // Arrange
        GivenInternalConfiguration();
        GivenTheConfiguration();
        _upstreamHeaders = new HeaderDictionary()
        {
            { "sec-ch-ua", "\"Microsoft Edge\";v=\"143\", \"Chromium\";v=\"143\", \"Not A(Brand\";v=\"24\"" },
            { "sec-ch-ua-mobile", "?0" },
        };
        _finder = new DiscoveryDownstreamRouteFinder(new RouteKeyCreator(), new UpstreamHeaderTemplatePatternCreator(), _dynamicRouting.Object);

        // Act
        WhenICreate();

        // Assert
        _result.Data.UpstreamHeaderTemplates.ShouldNotBeNull().ShouldBeEmpty("Should return empty collection for request headers");
        _result.Data.DownstreamRoute[0].UpstreamHeaders.ShouldNotBeNull().ShouldBeEmpty("Should return empty collection for request headers");
    }

    [Fact]
    [Trait("Bug", "2428")] // https://github.com/ThreeMammals/Ocelot/issues/2428
    public async Task Should_cache_one_downstream_route_for_concurrent_requests()
    {
        // Arrange
        const int Requests = 16;
        GivenInternalConfiguration();
        GivenTheConfiguration();
        using var barrier = new Barrier(Requests);
        DownstreamRoute WhenICreateConcurrently(int request)
        {
            barrier.SignalAndWait();
            return _finder.Get($"/auth/path{request}", _upstreamQuery, _upstreamHttpMethod, _configuration, _upstreamHost, _upstreamHeaders)
                .Data.DownstreamRoute[0];
        }

        // Act
        var routes = await Task.WhenAll(Enumerable.Range(0, Requests)
            .Select(request => Task.Factory.StartNew(() => WhenICreateConcurrently(request), TaskCreationOptions.LongRunning)));

        // Assert
        routes.Distinct().Count().ShouldBe(1, "Should cache one downstream route for concurrent requests");
    }

    private void ThenTheDownstreamRouteIsCreated(string serviceName = null, string serviceNamespace = null, string lbType = null, string lbKey = null)
    {
        var route = _result.Data;
        var downstreamRoute = route.DownstreamRoute[0];
        downstreamRoute.DownstreamPathTemplate.ShouldNotBeNull().Value.ShouldBe("/test");
        downstreamRoute.DownstreamScheme.ShouldBe("http");
        downstreamRoute.HttpHandlerOptions.ShouldBe(_handlerOptions);
        downstreamRoute.HttpHandlerOptions.ShouldNotBeNull();
        downstreamRoute.LoadBalancerKey.ShouldBe(lbKey ?? ".auth");
        downstreamRoute.LoadBalancerOptions.Type.ShouldBe(lbType ?? nameof(NoLoadBalancer));
        downstreamRoute.QosOptions.ShouldNotBeNull();
        downstreamRoute.QosOptions.ShouldNotBeNull();
        downstreamRoute.ServiceName.ShouldBe(serviceName ?? "auth");
        downstreamRoute.ServiceNamespace.ShouldBe(serviceNamespace ?? string.Empty);
        downstreamRoute.UpstreamPathTemplate.ShouldNotBeNull();
        downstreamRoute.UseServiceDiscovery.ShouldBeTrue();
        route.UpstreamHttpMethod.ShouldContain(HttpMethod.Get);
        route.UpstreamTemplatePattern.ShouldNotBeNull();
        var kv = _upstreamHeaders.First();
        route.UpstreamHeaderTemplates.ShouldNotBeNull()
            .FirstOrDefault(x => x.Key == kv.Key).Value.Template.ShouldBe(kv.Value);
        route.DownstreamRoute[0].UpstreamHeaders.ShouldNotBeNull()
            .FirstOrDefault(x => x.Key == kv.Key).Value.Template.ShouldBe(kv.Value);
    }

    private void ThenTheDownstreamPathIsForwardSlash()
    {
        var downstreamRoute = _result.Data.DownstreamRoute[0];
        downstreamRoute.DownstreamPathTemplate.Value.ShouldBe("/");
        downstreamRoute.ServiceName.ShouldBe("auth");
        downstreamRoute.ServiceNamespace.ShouldBeEmpty();
        downstreamRoute.LoadBalancerKey.ShouldBe(".auth");
    }

    private void GivenTheConfiguration()
    {
        _upstreamHost = "doesnt matter";
        _upstreamUrlPath = "/auth/test";
        _upstreamHttpMethod = "GET";
        _upstreamHeaders = new HeaderDictionary()
        {
            { "testHeader", "testHeaderValue" },
        };
        var kv = _upstreamHeaders.First();
        _headerTemplatePatternCreator.Setup(x => x.Create(It.IsAny<IHeaderDictionary>(), It.IsAny<bool>()))
            .Returns(new Dictionary<string, UpstreamHeaderTemplate>()
            {
                { kv.Key, new(kv.Value, kv.Value) },
            });
    }

    private void GivenInternalConfiguration(Route route = null, int index = 0)
    {
        var dr = route?.DownstreamRoute[index];
        _configuration = new InternalConfiguration(route is null ? null : [route])
        {
            AdministrationPath = "/AdminPath",
            RequestId = "requestID",
            LoadBalancerOptions = dr?.LoadBalancerOptions ?? _loadBalancerOptions,
            DownstreamScheme = (dr?.DownstreamScheme).IfEmpty(Uri.UriSchemeHttp),
            QoSOptions = dr?.QosOptions ?? _qoSOptions,
            HttpHandlerOptions = dr?.HttpHandlerOptions ?? _handlerOptions,
            DownstreamHttpVersion = dr?.DownstreamHttpVersion ?? new Version("1.1"),
            DownstreamHttpVersionPolicy = dr?.DownstreamHttpVersionPolicy ?? HttpVersionPolicy.RequestVersionOrLower,
            MetadataOptions = dr?.MetadataOptions ?? _metadataOptions,
            RateLimitOptions = dr?.RateLimitOptions ?? _rateLimitOptions,
            Timeout = dr?.Timeout ?? 111,
        };
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
