﻿using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.Provider;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class DownstreamRouteFinderMiddlewareTests
    {
        private readonly Mock<IDownstreamRouteFinder> _finder;
        private readonly Mock<IOcelotConfigurationProvider> _provider;
        private Response<DownstreamRoute> _downstreamRoute;
        private IOcelotConfiguration _config;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private DownstreamRouteFinderMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;
        private readonly Mock<IMultiplexer> _multiplexer;

        public DownstreamRouteFinderMiddlewareTests()
        {
            _provider = new Mock<IOcelotConfigurationProvider>();
            _finder = new Mock<IDownstreamRouteFinder>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<DownstreamRouteFinderMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _multiplexer = new Mock<IMultiplexer>();
            _middleware = new DownstreamRouteFinderMiddleware(_next, _loggerFactory.Object, _finder.Object, _provider.Object, _multiplexer.Object);
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            var config = new OcelotConfiguration(null, null, new ServiceProviderConfigurationBuilder().Build(), "");

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteFinderReturns(
                new DownstreamRoute(
                    new List<PlaceholderNameAndValue>(), 
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(downstreamReRoute)
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())))
                .And(x => GivenTheFollowingConfig(config))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetType();
        }

        private void GivenTheFollowingConfig(IOcelotConfiguration config)
        {
            _config = config;
            _provider
                .Setup(x => x.Get())
                .ReturnsAsync(new OkResponse<IOcelotConfiguration>(_config));
        }

        private void GivenTheDownStreamRouteFinderReturns(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _finder
                .Setup(x => x.FindDownstreamRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IOcelotConfiguration>(), It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _downstreamContext.TemplatePlaceholderNameAndValues.ShouldBe(_downstreamRoute.Data.TemplatePlaceholderNameAndValues);
            _downstreamContext.ServiceProviderConfiguration.ShouldBe(_config.ServiceProviderConfiguration);
        }
    }
}
