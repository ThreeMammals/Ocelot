using Ocelot.DownstreamRouteFinder.Finder;
using Xunit;
using Shouldly;
using Ocelot.Configuration;
using System.Net.Http;
using System.Linq;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    using System;
    using System.Threading.Tasks;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Repository;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DynamicConfigurationProvider;
    using Ocelot.LoadBalancer.LoadBalancers;
    using Ocelot.Logging;
    using Responses;
    using TestStack.BDDfy;

    public class DownstreamRouteCreatorTests
    {
        private readonly DownstreamRouteCreator _creator;
        private readonly QoSOptions _qoSOptions;
        private readonly RateLimitGlobalOptions _rateLimitGlobalOptions;
        private readonly HttpHandlerOptions _handlerOptions;
        private readonly LoadBalancerOptions _loadBalancerOptions;
        private readonly DynamicReRouteConfiguration _dynamicReRouteConfiguration;
        private readonly FileConfiguration _fileConfig;
        private Response<DownstreamRoute> _result;
        private string _upstreamHost;
        private string _upstreamUrlPath;
        private string _upstreamHttpMethod;
        private IInternalConfiguration _configuration;
        private DynamicConfigurationProvider _dynamicConfigurationProvider;
        private Mock<IQoSOptionsCreator> _qosOptionsCreator;
        private Mock<IRateLimitOptionsCreator> _rateLimitOptionsCreator;
        private Mock<IDynamicConfigurationProviderFactory> _dynamicConfigurationProviderFactory;
        private Mock<IOcelotLoggerFactory> _factory;
        private Mock<IOcelotLogger> _logger;
        private Response<DownstreamRoute> _resultTwo;
        private string _upstreamQuery;

        public DownstreamRouteCreatorTests()
        {
            _qosOptionsCreator = new Mock<IQoSOptionsCreator>();
            _rateLimitOptionsCreator = new Mock<IRateLimitOptionsCreator>();
            _dynamicConfigurationProviderFactory = new Mock<IDynamicConfigurationProviderFactory>();
            _factory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();

            _dynamicReRouteConfiguration = new DynamicConfigurationBuilder().Build();
            _qoSOptions = new QoSOptionsBuilder().Build();
            _handlerOptions = new HttpHandlerOptionsBuilder().Build();
            _rateLimitGlobalOptions = new RateLimitGlobalOptionsBuilder().Build();
            _loadBalancerOptions = new LoadBalancerOptionsBuilder().WithType(nameof(NoLoadBalancer)).Build();

            _factory.Setup(x => x.CreateLogger<DownstreamRouteCreator>()).Returns(_logger.Object);
            _qosOptionsCreator
                .Setup(x => x.Create(It.IsAny<QoSOptions>(), It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_qoSOptions);
            _creator = new DownstreamRouteCreator(_qosOptionsCreator.Object, 
                                                    _rateLimitOptionsCreator.Object,
                                                    _dynamicConfigurationProviderFactory.Object,
                                                    _factory.Object);
        }

        [Fact]
        public void should_create_downstream_route()
        {
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, _loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheDownstreamRouteIsCreated())
                .BDDfy();
        }

        [Fact]
        public void should_cache_downstream_route()
        {
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, _loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, "/geoffisthebest/"))
                .When(_ => WhenICreate())
                .And(_ => GivenTheConfiguration(configuration, "/geoffisthebest/"))
                .When(_ => WhenICreateAgain())
                .Then(_ => ThenTheDownstreamRoutesAreTheSameReference())
                .BDDfy();
        }

        [Fact]
        public void should_not_cache_downstream_route()
        {
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, _loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, "/geoffistheworst/"))
                .When(_ => WhenICreate())
                .And(_ => GivenTheConfiguration(configuration, "/geoffisthebest/"))
                .When(_ => WhenICreateAgain())
                .Then(_ => ThenTheDownstreamRoutesAreTheNotSameReference())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_no_path()
        {
            var upstreamUrlPath = "/auth/";
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _dynamicReRouteConfiguration, _loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, upstreamUrlPath))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheDownstreamPathIsForwardSlash())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_only_first_segment_no_traling_slash()
        {
            var upstreamUrlPath = "/auth";
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, _loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, upstreamUrlPath))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheDownstreamPathIsForwardSlash())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_segments_no_traling_slash()
        {
            var upstreamUrlPath = "/auth/test";
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, _loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, upstreamUrlPath))
                .When(_ => WhenICreate())
                .Then(_ => ThenThePathDoesNotHaveTrailingSlash())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_and_remove_query_string()
        {
            var upstreamUrlPath = "/auth/test?test=1&best=2";
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, _loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, upstreamUrlPath))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheQueryStringIsRemoved())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_for_sticky_sessions()
        {
            var loadBalancerOptions = new LoadBalancerOptionsBuilder().WithType(nameof(CookieStickySessions)).WithKey("boom").WithExpiryInMs(1).Build();
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheStickySessionLoadBalancerIsUsed(loadBalancerOptions))
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_qos()
        {
            var qoSOptions = new QoSOptionsBuilder()
                .WithExceptionsAllowedBeforeBreaking(1)
                .WithTimeoutValue(1)
                .Build();

            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, _loadBalancerOptions, "http", qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration))
                .And(_ => GivenTheQosCreatorReturns(qoSOptions))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheQosOptionsAreSet(qoSOptions))
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_rate_limit_and_dynamic_routing_config()
        {
            var rateLimitGlobalOptions = new RateLimitGlobalOptionsBuilder()
                .WithClientIdHeader("clientIdHeader")
                .WithDisableRateLimitHeaders(false)
                .WithQuotaExceededMessage("quotaExceededMessage")
                .WithRateLimitCounterPrefix("counterPrefix")
                .WithHttpStatusCode(500)
                .Build();

            var rateLimitOptions = new RateLimitOptionsBuilder()
                .WithClientIdHeader(rateLimitGlobalOptions.ClientIdHeader)
                .WithDisableRateLimitHeaders(rateLimitGlobalOptions.DisableRateLimitHeaders)
                .WithQuotaExceededMessage(rateLimitGlobalOptions.QuotaExceededMessage)
                .WithRateLimitCounterPrefix(rateLimitGlobalOptions.RateLimitCounterPrefix)
                .WithHttpStatusCode(rateLimitGlobalOptions.HttpStatusCode)
                .WithClientWhiteList(new[] { "client1", "client2" }.ToList())
                .WithEnableRateLimiting(true)
                .WithRateLimitRule(new RateLimitRule("1s", 2.5, 10))
                .Build();

            var dynamicRoutingConfiguration = new DynamicConfigurationBuilder()
                .WithServer("localhost", 1234)
                .WithStore("test")
                .Build();

            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", dynamicRoutingConfiguration, _loadBalancerOptions, "http", _qoSOptions, rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration))
                .And(_ => GivenTheRateLimitOptions(rateLimitOptions))
                .And(_ => GivenDynamicConfigurationProvider())
                .When(_ => WhenICreate())
                .Then(_ => ThenTheRateLimitOptionsAreSet(rateLimitOptions, true))
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_no_rate_limit_unreachable_store()
        {
            var rateLimitGlobalOptions = new RateLimitGlobalOptionsBuilder()
                .WithClientIdHeader("clientIdHeader")
                .WithDisableRateLimitHeaders(false)
                .WithQuotaExceededMessage("quotaExceededMessage")
                .WithRateLimitCounterPrefix("counterPrefix")
                .WithHttpStatusCode(500)
                .Build();

            var rateLimitOptions = new RateLimitOptionsBuilder().Build();

            var dynamicRoutingConfiguration = new DynamicConfigurationBuilder().Build();

            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", dynamicRoutingConfiguration, _loadBalancerOptions, "http", _qoSOptions, rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration))
                .And(_ => GivenTheRateLimitOptions(rateLimitOptions))
                .And(_ => GivenDynamicConfigurationProvider())
                .When(_ => WhenICreate())
                .Then(_ => ThenTheRateLimitOptionsAreSet(rateLimitOptions, false))
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_handler_options()
        {
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", null, _loadBalancerOptions, "http", _qoSOptions, _rateLimitGlobalOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheHandlerOptionsAreSet())
                .BDDfy();
        }

        private void GivenTheQosCreatorReturns(QoSOptions options)
        {
            _qosOptionsCreator
                .Setup(x => x.Create(It.IsAny<QoSOptions>(), It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(options);
        }

        private void GivenTheRateLimitOptions(RateLimitOptions options)
        {
            _rateLimitOptionsCreator.Setup(x => x.Create(It.IsAny<FileReRoute>(), It.IsAny<IInternalConfiguration>(), It.IsAny<bool>()))
                .Returns(options);
        }

        private void GivenDynamicConfigurationProvider()
        {
            _dynamicConfigurationProvider = new FakeProvider(_logger.Object, new FileReRoute() { RateLimitOptions = new FileRateLimitRule() { EnableRateLimiting = true } });
            _dynamicConfigurationProviderFactory.Setup(x => x.Get(It.IsAny<IInternalConfiguration>()))
                .Returns(_dynamicConfigurationProvider);
        }



        private void ThenTheDownstreamRouteIsCreated()
        {
            _result.Data.ReRoute.DownstreamReRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
            _result.Data.ReRoute.UpstreamHttpMethod[0].ShouldBe(HttpMethod.Get);
            _result.Data.ReRoute.DownstreamReRoute[0].ServiceName.ShouldBe("auth");
            _result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerKey.ShouldBe("/auth/test|GET");
            _result.Data.ReRoute.DownstreamReRoute[0].UseServiceDiscovery.ShouldBeTrue();
            _result.Data.ReRoute.DownstreamReRoute[0].HttpHandlerOptions.ShouldNotBeNull();
            _result.Data.ReRoute.DownstreamReRoute[0].QosOptions.ShouldNotBeNull();
            _result.Data.ReRoute.DownstreamReRoute[0].DownstreamScheme.ShouldBe("http");
            _result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerOptions.Type.ShouldBe(nameof(NoLoadBalancer));
            _result.Data.ReRoute.DownstreamReRoute[0].HttpHandlerOptions.ShouldBe(_handlerOptions);
            _result.Data.ReRoute.DownstreamReRoute[0].QosOptions.ShouldBe(_qoSOptions);
        }

        private void ThenTheDownstreamPathIsForwardSlash()
        {
            _result.Data.ReRoute.DownstreamReRoute[0].DownstreamPathTemplate.Value.ShouldBe("/");
            _result.Data.ReRoute.DownstreamReRoute[0].ServiceName.ShouldBe("auth");
            _result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerKey.ShouldBe("/auth/|GET");
        }

        private void ThenThePathDoesNotHaveTrailingSlash()
        {
            _result.Data.ReRoute.DownstreamReRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
            _result.Data.ReRoute.DownstreamReRoute[0].ServiceName.ShouldBe("auth");
            _result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerKey.ShouldBe("/auth/test|GET");
        }

        private void ThenTheQueryStringIsRemoved()
        {
            _result.Data.ReRoute.DownstreamReRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
            _result.Data.ReRoute.DownstreamReRoute[0].ServiceName.ShouldBe("auth");
            _result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerKey.ShouldBe("/auth/test|GET");
        }

        private void ThenTheStickySessionLoadBalancerIsUsed(LoadBalancerOptions expected)
        {
            _result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerKey.ShouldBe($"{nameof(CookieStickySessions)}:boom");
            _result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerOptions.Type.ShouldBe(nameof(CookieStickySessions));
            _result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerOptions.ShouldBe(expected);
        }

        private void ThenTheQosOptionsAreSet(QoSOptions expected)
        {
            _result.Data.ReRoute.DownstreamReRoute[0].QosOptions.ShouldBe(expected);
            _result.Data.ReRoute.DownstreamReRoute[0].QosOptions.UseQos.ShouldBeTrue();
            _qosOptionsCreator
                .Verify(x => x.Create(expected, _upstreamUrlPath, It.IsAny<string[]>()), Times.Once);
        }

        private void ThenTheRateLimitOptionsAreSet(RateLimitOptions expected, bool expectedRateLimitState)
        {
            _result.Data.ReRoute.DownstreamReRoute[0].RateLimitOptions.ShouldBe(expected);
            _result.Data.ReRoute.DownstreamReRoute[0].RateLimitOptions.EnableRateLimiting.ShouldBe(expectedRateLimitState);
        }

        private void GivenTheConfiguration(IInternalConfiguration config)
        {
            _upstreamHost = "doesnt matter";
            _upstreamUrlPath = "/auth/test";
            _upstreamHttpMethod = "GET";
            _configuration = config;
        }

        private void GivenTheConfiguration(IInternalConfiguration config, string upstreamUrlPath)
        {
            _upstreamHost = "doesnt matter";
            _upstreamUrlPath = upstreamUrlPath;
            _upstreamHttpMethod = "GET";
            _configuration = config;
        }

        private void ThenTheHandlerOptionsAreSet()
        {
            _result.Data.ReRoute.DownstreamReRoute[0].HttpHandlerOptions.ShouldBe(_handlerOptions);
        }

        private void WhenICreate()
        {
            _result = _creator.GetAsync(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _configuration, _upstreamHost).GetAwaiter().GetResult();
        }

        private void WhenICreateAgain()
        {
            _resultTwo = _creator.GetAsync(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _configuration, _upstreamHost).GetAwaiter().GetResult();
        }

        private void ThenTheDownstreamRoutesAreTheSameReference()
        {
            _result.ShouldBe(_resultTwo);
        }

        private void ThenTheDownstreamRoutesAreTheNotSameReference()
        {
            _result.ShouldNotBe(_resultTwo);
        }

        public class FakeProvider : DynamicConfigurationProvider
        {
            private readonly FileReRoute _reRoute;

            public FakeProvider(IOcelotLogger logger, FileReRoute reRoute) : base(logger)
            {
                _reRoute = reRoute;
            }

            protected async override Task<FileReRoute> GetRouteConfigurationAsync(string host, string port, string key)
            {
                return await Task.FromResult(_reRoute);
            }
        }
    }
}
