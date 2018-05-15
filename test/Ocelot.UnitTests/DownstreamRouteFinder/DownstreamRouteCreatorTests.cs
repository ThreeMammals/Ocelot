using Ocelot.DownstreamRouteFinder.Finder;
using Xunit;
using Shouldly;
using Ocelot.Configuration;
using System.Net.Http;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    using System;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.Creator;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.LoadBalancer.LoadBalancers;
    using Responses;
    using TestStack.BDDfy;

    public class DownstreamRouteCreatorTests
    {
        private readonly DownstreamRouteCreator _creator;
        private readonly QoSOptions _qoSOptions;
        private readonly HttpHandlerOptions _handlerOptions;
        private readonly LoadBalancerOptions _loadBalancerOptions;
        private Response<DownstreamRoute> _result;
        private string _upstreamHost;
        private string _upstreamUrlPath;
        private string _upstreamHttpMethod;
        private IInternalConfiguration _configuration;
        private Mock<IQoSOptionsCreator> _qosOptionsCreator;
        private Response<DownstreamRoute> _resultTwo;

        public DownstreamRouteCreatorTests()
        {
            _qosOptionsCreator = new Mock<IQoSOptionsCreator>();
            _qoSOptions = new QoSOptionsBuilder().Build();
            _handlerOptions = new HttpHandlerOptionsBuilder().Build();
            _loadBalancerOptions = new LoadBalancerOptionsBuilder().WithType(nameof(NoLoadBalancer)).Build();
            _qosOptionsCreator
                .Setup(x => x.Create(It.IsAny<QoSOptions>(), It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_qoSOptions);
            _creator = new DownstreamRouteCreator(_qosOptionsCreator.Object);
        }

        [Fact]
        public void should_create_downstream_route()
        {
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheDownstreamRouteIsCreated())
                .BDDfy();
        }

        [Fact]
        public void should_cache_downstream_route()
        {
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

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
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

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
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, upstreamUrlPath))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheDownstreamPathIsForwardSlash())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_only_first_segment_no_traling_slash()
        {
            var upstreamUrlPath = "/auth";
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, upstreamUrlPath))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheDownstreamPathIsForwardSlash())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_segments_no_traling_slash()
        {
            var upstreamUrlPath = "/auth/test";
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, upstreamUrlPath))
                .When(_ => WhenICreate())
                .Then(_ => ThenThePathDoesNotHaveTrailingSlash())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_and_remove_query_string()
        {
            var upstreamUrlPath = "/auth/test?test=1&best=2";
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration, upstreamUrlPath))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheQueryStringIsRemoved())
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_for_sticky_sessions()
        {
            var loadBalancerOptions = new LoadBalancerOptionsBuilder().WithType(nameof(CookieStickySessions)).WithKey("boom").WithExpiryInMs(1).Build();
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

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

            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", qoSOptions, _handlerOptions);

            this.Given(_ => GivenTheConfiguration(configuration))
                .And(_ => GivenTheQosCreatorReturns(qoSOptions))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheQosOptionsAreSet(qoSOptions))
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_route_with_handler_options()
        {
            var configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter", _loadBalancerOptions, "http", _qoSOptions, _handlerOptions);

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
            _result = _creator.Get(_upstreamUrlPath, _upstreamHttpMethod, _configuration, _upstreamHost);
        }

        private void WhenICreateAgain()
        {
            _resultTwo = _creator.Get(_upstreamUrlPath, _upstreamHttpMethod, _configuration, _upstreamHost);
        }

        private void ThenTheDownstreamRoutesAreTheSameReference()
        {
            _result.ShouldBe(_resultTwo);
        }

        private void ThenTheDownstreamRoutesAreTheNotSameReference()
        {
            _result.ShouldNotBe(_resultTwo);
        }
    }
}
