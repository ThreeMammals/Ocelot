﻿using Ocelot.Middleware;

namespace Ocelot.UnitTests.RequestId
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using Ocelot.Infrastructure.RequestData;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
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

    public class ReRouteRequestIdMiddlewareTests
    {
        private readonly HttpRequestMessage _downstreamRequest;
        private string _value;
        private string _key;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly ReRouteRequestIdMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;
        private readonly Mock<IRequestScopedDataRepository> _repo;

        public ReRouteRequestIdMiddlewareTests()
        {
            _downstreamRequest = new HttpRequestMessage();
            _repo = new Mock<IRequestScopedDataRepository>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<ReRouteRequestIdMiddleware>()).Returns(_logger.Object);
            _next = context =>
            {
                context.HttpContext.Response.Headers.Add("LSRequestId", context.HttpContext.TraceIdentifier);
                return Task.CompletedTask;
            };
            _middleware = new ReRouteRequestIdMiddleware(_next, _loggerFactory.Object, _repo.Object);
            _downstreamContext.DownstreamRequest = _downstreamRequest;
        }

        [Fact]
        public void should_pass_down_request_id_from_upstream_request()
        {
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithRequestIdKey("LSRequestId")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())
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
                    .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithRequestIdKey("LSRequestId")
                        .WithUpstreamHttpMethod(new List<string> {"Get"})
                        .Build())
                    .WithUpstreamHttpMethod(new List<string> {"Get"})
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
                    .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithRequestIdKey("LSRequestId")
                        .WithUpstreamHttpMethod(new List<string> {"Get"})
                        .Build())
                    .WithUpstreamHttpMethod(new List<string> {"Get"})
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
                    .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithRequestIdKey("LSRequestId")
                        .WithUpstreamHttpMethod(new List<string> {"Get"})
                        .Build())
                    .WithUpstreamHttpMethod(new List<string> {"Get"})
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

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenThereIsNoGlobalRequestId()
        {
            _repo.Setup(x => x.Get<string>("RequestId")).Returns(new OkResponse<string>(null));
        }

        private void GivenTheRequestIdWasSetGlobally()
        {
            _repo.Setup(x => x.Get<string>("RequestId")).Returns(new OkResponse<string>("alreadyset"));
        }

        private void ThenTheRequestIdIsSaved()
        {
            _repo.Verify(x => x.Add<string>("RequestId", _value), Times.Once);
        }

        private void ThenTheRequestIdIsUpdated()
        {
            _repo.Verify(x => x.Update<string>("RequestId", _value), Times.Once);
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamContext.TemplatePlaceholderNameAndValues = downstreamRoute.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }

        private void GivenTheRequestIdIsAddedToTheRequest(string key, string value)
        {
            _key = key;
            _value = value;
            _downstreamContext.HttpContext.Request.Headers.TryAdd(_key, _value);
        }

        private void ThenTheTraceIdIsAnything()
        {
            StringValues value;
            _downstreamContext.HttpContext.Response.Headers.TryGetValue("LSRequestId", out value);
            value.First().ShouldNotBeNullOrEmpty();
        }

        private void ThenTheTraceIdIs(string expected)
        {
            StringValues value;
            _downstreamContext.HttpContext.Response.Headers.TryGetValue("LSRequestId", out value);
            value.First().ShouldBe(expected);
        }
    }
}
