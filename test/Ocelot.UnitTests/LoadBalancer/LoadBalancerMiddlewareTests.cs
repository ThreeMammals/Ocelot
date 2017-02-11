using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Errors;
using Ocelot.Infrastructure.RequestData;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.Values;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class LoadBalancerMiddlewareTests
    {
        private readonly Mock<ILoadBalancerHouse> _loadBalancerHouse;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<ILoadBalancer> _loadBalancer;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private HostAndPort _hostAndPort;
        private OkResponse<string> _downstreamUrl;
        private OkResponse<DownstreamRoute> _downstreamRoute;
        private ErrorResponse<ILoadBalancer> _getLoadBalancerHouseError;
        private ErrorResponse<HostAndPort> _getHostAndPortError;

        public LoadBalancerMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            _loadBalancer = new Mock<ILoadBalancer>();
            _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_loadBalancerHouse.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseLoadBalancingMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.UrlPathPlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithUpstreamHttpMethod("Get")
                    .Build());

            this.Given(x => x.GivenTheDownStreamUrlIs("any old string"))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheLoadBalancerHouseReturns())
                .And(x => x.GivenTheLoadBalancerReturns())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_set_pipeline_error_if_cannot_get_load_balancer()
        {         
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.UrlPathPlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithUpstreamHttpMethod("Get")
                    .Build());

            this.Given(x => x.GivenTheDownStreamUrlIs("any old string"))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheLoadBalancerHouseReturnsAnError())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenAnErrorStatingLoadBalancerCouldNotBeFoundIsSetOnPipeline())
                .BDDfy();
        }

        [Fact]
        public void should_set_pipeline_error_if_cannot_get_least()
        {
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.UrlPathPlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithUpstreamHttpMethod("Get")
                    .Build());

            this.Given(x => x.GivenTheDownStreamUrlIs("any old string"))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheLoadBalancerHouseReturns())
                .And(x => x.GivenTheLoadBalancerReturnsAnError())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenAnErrorStatingHostAndPortCouldNotBeFoundIsSetOnPipeline())
                .BDDfy();
        }

        private void GivenTheLoadBalancerReturnsAnError()
        {
            _getHostAndPortError = new ErrorResponse<HostAndPort>(new List<Error>() { new ServicesAreNullError($"services were null for bah") });
             _loadBalancer
                .Setup(x => x.Lease())
                .ReturnsAsync(_getHostAndPortError);
        }

        private void GivenTheLoadBalancerReturns()
        {
            _hostAndPort = new HostAndPort("127.0.0.1", 80);
            _loadBalancer
                .Setup(x => x.Lease())
                .ReturnsAsync(new OkResponse<HostAndPort>(_hostAndPort));
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _scopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void GivenTheLoadBalancerHouseReturns()
        {
            _loadBalancerHouse
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new OkResponse<ILoadBalancer>(_loadBalancer.Object));
        }


        private void GivenTheLoadBalancerHouseReturnsAnError()
        {
            _getLoadBalancerHouseError = new ErrorResponse<ILoadBalancer>(new List<Ocelot.Errors.Error>()
            {
                new UnableToFindLoadBalancerError($"unabe to find load balancer for bah")
            });

            _loadBalancerHouse
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(_getLoadBalancerHouseError);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _scopedRepository
                .Verify(x => x.Add("HostAndPort", _hostAndPort), Times.Once());
        }

        private void ThenAnErrorStatingLoadBalancerCouldNotBeFoundIsSetOnPipeline()
        {
            _scopedRepository
                .Verify(x => x.Add("OcelotMiddlewareError", true), Times.Once);

            _scopedRepository
                .Verify(x => x.Add("OcelotMiddlewareErrors", _getLoadBalancerHouseError.Errors), Times.Once);
        }

         private void ThenAnErrorSayingReleaseFailedIsSetOnThePipeline()
        {
            _scopedRepository
                .Verify(x => x.Add("OcelotMiddlewareError", true), Times.Once);

            _scopedRepository
                .Verify(x => x.Add("OcelotMiddlewareErrors", It.IsAny<List<Error>>()), Times.Once);
        }

            private void ThenAnErrorStatingHostAndPortCouldNotBeFoundIsSetOnPipeline()
        {
            _scopedRepository
                .Verify(x => x.Add("OcelotMiddlewareError", true), Times.Once);

            _scopedRepository
                .Verify(x => x.Add("OcelotMiddlewareErrors", _getHostAndPortError.Errors), Times.Once);
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheDownStreamUrlIs(string downstreamUrl)
        {
            _downstreamUrl = new OkResponse<string>(downstreamUrl);
            _scopedRepository
                .Setup(x => x.Get<string>(It.IsAny<string>()))
                .Returns(_downstreamUrl);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}