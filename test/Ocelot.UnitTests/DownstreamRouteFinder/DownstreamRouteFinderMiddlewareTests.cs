﻿using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.UnitTests.DownstreamRouteFinder;

public class DownstreamRouteFinderMiddlewareTests : UnitTest
{
    private readonly Mock<IDownstreamRouteProvider> _finder;
    private readonly Mock<IDownstreamRouteProviderFactory> _factory;
    private Response<DownstreamRouteHolder> _downstreamRoute;
    private IInternalConfiguration _config;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly DownstreamRouteFinderMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public DownstreamRouteFinderMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _finder = new Mock<IDownstreamRouteProvider>();
        _factory = new Mock<IDownstreamRouteProviderFactory>();
        _factory.Setup(x => x.Get(It.IsAny<IInternalConfiguration>())).Returns(_finder.Object);
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<DownstreamRouteFinderMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new DownstreamRouteFinderMiddleware(_next, _loggerFactory.Object, _factory.Object);
    }

    [Fact]
    public async Task Should_call_scoped_data_repository_correctly()
    {
        // Arrange
        var config = new InternalConfiguration(
            null,
            null,
            new ServiceProviderConfigurationBuilder().Build(),
            string.Empty,
            new LoadBalancerOptionsBuilder().Build(),
            string.Empty,
            new QoSOptionsBuilder().Build(),
            new HttpHandlerOptionsBuilder().Build(),
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("any old string")
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .Build();
        GivenTheDownStreamRouteFinderReturns(new(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()));
        GivenTheFollowingConfig(config);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenTheScopedDataRepositoryIsCalledCorrectly();
    }

    private void GivenTheFollowingConfig(IInternalConfiguration config)
    {
        _config = config;
        _httpContext.Items.SetIInternalConfiguration(config);
    }

    private void GivenTheDownStreamRouteFinderReturns(DownstreamRouteHolder downstreamRoute)
    {
        _downstreamRoute = new OkResponse<DownstreamRouteHolder>(downstreamRoute);
        _finder
            .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IInternalConfiguration>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(_downstreamRoute);
    }

    private void ThenTheScopedDataRepositoryIsCalledCorrectly()
    {
        _httpContext.Items.TemplatePlaceholderNameAndValues().ShouldBe(_downstreamRoute.Data.TemplatePlaceholderNameAndValues);
        _httpContext.Items.IInternalConfiguration().ServiceProviderConfiguration.ShouldBe(_config.ServiceProviderConfiguration);
    }
}
