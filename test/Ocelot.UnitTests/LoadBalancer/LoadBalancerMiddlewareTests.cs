using Ocelot.Middleware;

namespace Ocelot.UnitTests.LoadBalancer
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Errors;
    using Ocelot.LoadBalancer.LoadBalancers;
    using Ocelot.LoadBalancer.Middleware;
    using Ocelot.Logging;
    using Ocelot.Request.Middleware;
    using Ocelot.Responses;
    using Ocelot.Values;
    using Shouldly;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestStack.BDDfy;
    using Xunit;

    public class LoadBalancerMiddlewareTests
    {
        private readonly Mock<ILoadBalancerHouse> _loadBalancerHouse;
        private readonly Mock<ILoadBalancer> _loadBalancer;
        private ServiceHostAndPort _hostAndPort;
        private ErrorResponse<ILoadBalancer> _getLoadBalancerHouseError;
        private ErrorResponse<ServiceHostAndPort> _getHostAndPortError;
        private HttpRequestMessage _downstreamRequest;
        private ServiceProviderConfiguration _config;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private LoadBalancingMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public LoadBalancerMiddlewareTests()
        {
            _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
            _loadBalancer = new Mock<ILoadBalancer>();
            _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
            _downstreamRequest = new HttpRequestMessage(HttpMethod.Get, "http://test.com/");
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<LoadBalancingMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _downstreamContext.DownstreamRequest = new DownstreamRequest(_downstreamRequest);
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            var downstreamRoute = new DownstreamReRouteBuilder()
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build();

            var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamUrlIs("http://my.url/abc?q=123"))
                .And(x => GivenTheConfigurationIs(serviceProviderConfig))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute, new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>()))
                .And(x => x.GivenTheLoadBalancerHouseReturns())
                .And(x => x.GivenTheLoadBalancerReturns())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamUrlIsReplacedWith("http://127.0.0.1:80/abc?q=123"))
                .BDDfy();
        }

        [Fact]
        public void should_set_pipeline_error_if_cannot_get_load_balancer()
        {
            var downstreamRoute = new DownstreamReRouteBuilder()
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build();

            var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamUrlIs("http://my.url/abc?q=123"))
                .And(x => GivenTheConfigurationIs(serviceProviderConfig))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute, new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>()))
                .And(x => x.GivenTheLoadBalancerHouseReturnsAnError())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenAnErrorStatingLoadBalancerCouldNotBeFoundIsSetOnPipeline())
                .BDDfy();
        }

        [Fact]
        public void should_set_pipeline_error_if_cannot_get_least()
        {
            var downstreamRoute = new DownstreamReRouteBuilder()
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build();

            var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
               .Build();

            this.Given(x => x.GivenTheDownStreamUrlIs("http://my.url/abc?q=123"))
                .And(x => GivenTheConfigurationIs(serviceProviderConfig))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute, new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>()))
                .And(x => x.GivenTheLoadBalancerHouseReturns())
                .And(x => x.GivenTheLoadBalancerReturnsAnError())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenAnErrorStatingHostAndPortCouldNotBeFoundIsSetOnPipeline())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware = new LoadBalancingMiddleware(_next, _loggerFactory.Object, _loadBalancerHouse.Object);
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheConfigurationIs(ServiceProviderConfiguration config)
        {
            _config = config;
            var configuration = new InternalConfiguration(null, null, config, null, null, null, null, null);
            _downstreamContext.Configuration = configuration;
        }

        private void GivenTheDownStreamUrlIs(string downstreamUrl)
        {
            _downstreamRequest.RequestUri = new System.Uri(downstreamUrl);
            _downstreamContext.DownstreamRequest = new DownstreamRequest(_downstreamRequest);
        }

        private void GivenTheLoadBalancerReturnsAnError()
        {
            _getHostAndPortError = new ErrorResponse<ServiceHostAndPort>(new List<Error>() { new ServicesAreNullError($"services were null for bah") });
            _loadBalancer
               .Setup(x => x.Lease(It.IsAny<DownstreamContext>()))
               .ReturnsAsync(_getHostAndPortError);
        }

        private void GivenTheLoadBalancerReturns()
        {
            _hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);
            _loadBalancer
                .Setup(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(_hostAndPort));
        }

        private void GivenTheDownStreamRouteIs(DownstreamReRoute downstreamRoute, List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue> placeholder)
        {
            _downstreamContext.TemplatePlaceholderNameAndValues = placeholder;
            _downstreamContext.DownstreamReRoute = downstreamRoute;
        }

        private void GivenTheLoadBalancerHouseReturns()
        {
            _loadBalancerHouse
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>(), It.IsAny<ServiceProviderConfiguration>()))
                .ReturnsAsync(new OkResponse<ILoadBalancer>(_loadBalancer.Object));
        }

        private void GivenTheLoadBalancerHouseReturnsAnError()
        {
            _getLoadBalancerHouseError = new ErrorResponse<ILoadBalancer>(new List<Ocelot.Errors.Error>()
            {
                new UnableToFindLoadBalancerError($"unabe to find load balancer for bah")
            });

            _loadBalancerHouse
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>(), It.IsAny<ServiceProviderConfiguration>()))
                .ReturnsAsync(_getLoadBalancerHouseError);
        }

        private void ThenAnErrorStatingLoadBalancerCouldNotBeFoundIsSetOnPipeline()
        {
            _downstreamContext.IsError.ShouldBeTrue();
            _downstreamContext.Errors.ShouldBe(_getLoadBalancerHouseError.Errors);
        }

        private void ThenAnErrorSayingReleaseFailedIsSetOnThePipeline()
        {
            _downstreamContext.IsError.ShouldBeTrue();
            _downstreamContext.Errors.ShouldBe(It.IsAny<List<Error>>());
        }

        private void ThenAnErrorStatingHostAndPortCouldNotBeFoundIsSetOnPipeline()
        {
            _downstreamContext.IsError.ShouldBeTrue();
            _downstreamContext.Errors.ShouldBe(_getHostAndPortError.Errors);
        }

        private void ThenTheDownstreamUrlIsReplacedWith(string expectedUri)
        {
            _downstreamContext.DownstreamRequest.ToHttpRequestMessage().RequestUri.OriginalString.ShouldBe(expectedUri);
        }
    }
}
