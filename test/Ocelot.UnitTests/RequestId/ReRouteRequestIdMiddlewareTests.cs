namespace Ocelot.UnitTests.RequestId
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.RequestId.Middleware;
    using Ocelot.Responses;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class ReRouteRequestIdMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly HttpRequestMessage _downstreamRequest;
        private Response<DownstreamRoute> _downstreamRoute;
        private string _value;
        private string _key;

        public ReRouteRequestIdMiddlewareTests()
        {
            _downstreamRequest = new HttpRequestMessage();

            ScopedRepository
                .Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(_downstreamRequest));

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_pass_down_request_id_from_upstream_request()
        {
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithRequestIdKey("LSRequestId")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

            var requestId = Guid.NewGuid().ToString();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => GivenThereIsNoGlobalRequestId())
                .And(x => x.GivenTheRequestIdIsAddedToTheRequest("LSRequestId", requestId))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheTraceIdIs(requestId))
                .BDDfy();
        }

        [Fact]
        public void should_add_request_id_when_not_on_upstream_request()
        {
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithRequestIdKey("LSRequestId")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => GivenThereIsNoGlobalRequestId())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheTraceIdIsAnything())
                .BDDfy();
        }

        [Fact]
        public void should_add_request_id_scoped_repo_for_logging_later()
        {
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithRequestIdKey("LSRequestId")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

            var requestId = Guid.NewGuid().ToString();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => GivenThereIsNoGlobalRequestId())
                .And(x => x.GivenTheRequestIdIsAddedToTheRequest("LSRequestId", requestId))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheTraceIdIs(requestId))
                .And(x => ThenTheRequestIdIsSaved())
                .BDDfy();
        }

        [Fact]
        public void should_update_request_id_scoped_repo_for_logging_later()
        {
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithRequestIdKey("LSRequestId")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

            var requestId = Guid.NewGuid().ToString();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => GivenTheRequestIdWasSetGlobally())
                .And(x => x.GivenTheRequestIdIsAddedToTheRequest("LSRequestId", requestId))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheTraceIdIs(requestId))
                .And(x => ThenTheRequestIdIsUpdated())
                .BDDfy();
        }

        private void GivenThereIsNoGlobalRequestId()
        {
            ScopedRepository.Setup(x => x.Get<string>("RequestId")).Returns(new OkResponse<string>(null));
        }

        private void GivenTheRequestIdWasSetGlobally()
        {
            ScopedRepository.Setup(x => x.Get<string>("RequestId")).Returns(new OkResponse<string>("alreadyset"));
        }

        private void ThenTheRequestIdIsSaved()
        {
            ScopedRepository.Verify(x => x.Add<string>("RequestId", _value), Times.Once);
        }

        private void ThenTheRequestIdIsUpdated()
        {
            ScopedRepository.Verify(x => x.Update<string>("RequestId", _value), Times.Once);
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseRequestIdMiddleware();

            app.Run(x =>
            {
                x.Response.Headers.Add("LSRequestId", x.TraceIdentifier);
                return Task.CompletedTask;
            });
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            ScopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void GivenTheRequestIdIsAddedToTheRequest(string key, string value)
        {
            _key = key;
            _value = value;
            Client.DefaultRequestHeaders.TryAddWithoutValidation(_key, _value);
        }

        private void ThenTheTraceIdIsAnything()
        {
            ResponseMessage.Headers.GetValues("LSRequestId").First().ShouldNotBeNullOrEmpty();
        }

        private void ThenTheTraceIdIs(string expected)
        {
            ResponseMessage.Headers.GetValues("LSRequestId").First().ShouldBe(expected);
        }
    }
}
