namespace Ocelot.UnitTests.LoadBalancer
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.Provider;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.Errors;
    using Ocelot.LoadBalancer.LoadBalancers;
    using Ocelot.LoadBalancer.Middleware;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using Ocelot.Values;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class LoadBalancerMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly Mock<ILoadBalancerHouse> _loadBalancerHouse;
        private readonly Mock<ILoadBalancer> _loadBalancer;
        private ServiceHostAndPort _hostAndPort;
        private OkResponse<DownstreamRoute> _downstreamRoute;
        private ErrorResponse<ILoadBalancer> _getLoadBalancerHouseError;
        private ErrorResponse<ServiceHostAndPort> _getHostAndPortError;
        private HttpRequestMessage _downstreamRequest;
        private ServiceProviderConfiguration _config;

        public LoadBalancerMiddlewareTests()
        {
            _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
            _loadBalancer = new Mock<ILoadBalancer>();
            _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
            _downstreamRequest = new HttpRequestMessage(HttpMethod.Get, "");

            ScopedRepository
                .Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(_downstreamRequest));

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build());

            var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamUrlIs("http://my.url/abc?q=123"))
                .And(x => GivenTheConfigurationIs(serviceProviderConfig))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheLoadBalancerHouseReturns())
                .And(x => x.GivenTheLoadBalancerReturns())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamUrlIsReplacedWith("http://127.0.0.1:80/abc?q=123"))
                .BDDfy();
        }

        [Fact]
        public void should_set_pipeline_error_if_cannot_get_load_balancer()
        {         
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build());

            var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamUrlIs("http://my.url/abc?q=123"))
                .And(x => GivenTheConfigurationIs(serviceProviderConfig))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheLoadBalancerHouseReturnsAnError())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenAnErrorStatingLoadBalancerCouldNotBeFoundIsSetOnPipeline())
                .BDDfy();
        }

        [Fact]
        public void should_set_pipeline_error_if_cannot_get_least()
        {
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build());
                
             var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamUrlIs("http://my.url/abc?q=123"))
                .And(x => GivenTheConfigurationIs(serviceProviderConfig))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheLoadBalancerHouseReturns())
                .And(x => x.GivenTheLoadBalancerReturnsAnError())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenAnErrorStatingHostAndPortCouldNotBeFoundIsSetOnPipeline())
                .BDDfy();
        }

        private void GivenTheConfigurationIs(ServiceProviderConfiguration config)
        {
            _config = config;
            ScopedRepository
                .Setup(x => x.Get<ServiceProviderConfiguration>("ServiceProviderConfiguration")).Returns(new OkResponse<ServiceProviderConfiguration>(config));
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_loadBalancerHouse.Object);
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseLoadBalancingMiddleware();
        }

        private void GivenTheDownStreamUrlIs(string downstreamUrl)
        {
            _downstreamRequest.RequestUri = new System.Uri(downstreamUrl);
        }

        private void GivenTheLoadBalancerReturnsAnError()
        {
            _getHostAndPortError = new ErrorResponse<ServiceHostAndPort>(new List<Error>() { new ServicesAreNullError($"services were null for bah") });
             _loadBalancer
                .Setup(x => x.Lease())
                .ReturnsAsync(_getHostAndPortError);
        }

        private void GivenTheLoadBalancerReturns()
        {
            _hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);
            _loadBalancer
                .Setup(x => x.Lease())
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(_hostAndPort));
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            ScopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void GivenTheLoadBalancerHouseReturns()
        {
            _loadBalancerHouse
                .Setup(x => x.Get(It.IsAny<ReRoute>(), It.IsAny<ServiceProviderConfiguration>()))
                .ReturnsAsync(new OkResponse<ILoadBalancer>(_loadBalancer.Object));
        }

        private void GivenTheLoadBalancerHouseReturnsAnError()
        {
            _getLoadBalancerHouseError = new ErrorResponse<ILoadBalancer>(new List<Ocelot.Errors.Error>()
            {
                new UnableToFindLoadBalancerError($"unabe to find load balancer for bah")
            });

            _loadBalancerHouse
                .Setup(x => x.Get(It.IsAny<ReRoute>(), It.IsAny<ServiceProviderConfiguration>()))
                .ReturnsAsync(_getLoadBalancerHouseError);
        }

        private void ThenAnErrorStatingLoadBalancerCouldNotBeFoundIsSetOnPipeline()
        {
            ScopedRepository
                .Verify(x => x.Add("OcelotMiddlewareError", true), Times.Once);

            ScopedRepository
                .Verify(x => x.Add("OcelotMiddlewareErrors", _getLoadBalancerHouseError.Errors), Times.Once);
        }

        private void ThenAnErrorSayingReleaseFailedIsSetOnThePipeline()
        {
            ScopedRepository
                .Verify(x => x.Add("OcelotMiddlewareError", true), Times.Once);

            ScopedRepository
                .Verify(x => x.Add("OcelotMiddlewareErrors", It.IsAny<List<Error>>()), Times.Once);
        }

        private void ThenAnErrorStatingHostAndPortCouldNotBeFoundIsSetOnPipeline()
        {
            ScopedRepository
                .Verify(x => x.Add("OcelotMiddlewareError", true), Times.Once);

            ScopedRepository
                .Verify(x => x.Add("OcelotMiddlewareErrors", _getHostAndPortError.Errors), Times.Once);
        }

        private void ThenTheDownstreamUrlIsReplacedWith(string expectedUri)
        {
            _downstreamRequest.RequestUri.OriginalString.ShouldBe(expectedUri);
        }
    }
}
