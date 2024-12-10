﻿using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Creator;
using Ocelot.Request.Mapper;
using Ocelot.Request.Middleware;

namespace Ocelot.UnitTests.Request;

public class DownstreamRequestInitialiserMiddlewareTests : UnitTest
{
    private readonly DownstreamRequestInitialiserMiddleware _middleware;
    private readonly HttpContext _httpContext;
    private readonly Mock<RequestDelegate> _next;
    private readonly Mock<IRequestMapper> _requestMapper;
    private HttpRequestMessage _mappedRequest;
    private readonly Exception _testException;

    public DownstreamRequestInitialiserMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _requestMapper = new Mock<IRequestMapper>();
        _next = new Mock<RequestDelegate>();
        var logger = new Mock<IOcelotLogger>();
        _testException = new Exception("test exception");

        var loggerFactory = new Mock<IOcelotLoggerFactory>();
        loggerFactory
            .Setup(lf => lf.CreateLogger<DownstreamRequestInitialiserMiddleware>())
            .Returns(logger.Object);

        _middleware = new DownstreamRequestInitialiserMiddleware(
            _next.Object,
            loggerFactory.Object,
            _requestMapper.Object,
            new DownstreamRequestCreator(new FrameworkDescription()));
    }

    [Fact]
    public async Task Should_handle_valid_httpRequest()
    {
        GivenTheHttpContextContainsARequest();
        GivenTheMapperWillReturnAMappedRequest();
        await WhenTheMiddlewareIsInvoked();
        ThenTheContexRequestIsMappedToADownstreamRequest();
        ThenTheDownstreamRequestIsStored();
        ThenTheNextMiddlewareIsInvoked();
        ThenTheDownstreamRequestMethodIs("GET");
    }

    [Fact]
    public async Task Should_map_downstream_route_method_to_downstream_request()
    {
        GivenTheHttpContextContainsARequest();
        GivenTheMapperWillReturnAMappedRequest();
        await WhenTheMiddlewareIsInvoked();
        ThenTheContexRequestIsMappedToADownstreamRequest();
        ThenTheDownstreamRequestIsStored();
        ThenTheNextMiddlewareIsInvoked();
        ThenTheDownstreamRequestMethodIs("GET");
    }

    [Fact]
    public async Task Should_handle_mapping_failure()
    {
        GivenTheHttpContextContainsARequest();
        GivenTheMapperWillReturnAnError();
        await WhenTheMiddlewareIsInvoked();
        ThenTheDownstreamRequestIsNotStored();
        ThenAPipelineErrorIsStored();
        ThenTheNextMiddlewareIsNotInvoked();
    }

    private void ThenTheDownstreamRequestMethodIs(string expected)
    {
        _httpContext.Items.DownstreamRequest().Method.ShouldBe(expected);
    }

    private void GivenTheHttpContextContainsARequest()
    {
        _httpContext.Items.UpsertDownstreamRoute(new DownstreamRouteBuilder().Build());
    }

    private void GivenTheMapperWillReturnAMappedRequest()
    {
        _mappedRequest = new HttpRequestMessage(HttpMethod.Get, "http://www.bbc.co.uk");

        _requestMapper
            .Setup(rm => rm.Map(It.IsAny<HttpRequest>(), It.IsAny<DownstreamRoute>()))
            .Returns(_mappedRequest);
    }

    private void GivenTheMapperWillReturnAnError()
    {
        _requestMapper
            .Setup(rm => rm.Map(It.IsAny<HttpRequest>(), It.IsAny<DownstreamRoute>()))
            .Throws(_testException);
    }

    private async Task WhenTheMiddlewareIsInvoked()
    {
        await _middleware.Invoke(_httpContext);
    }

    private void ThenTheContexRequestIsMappedToADownstreamRequest()
    {
        _requestMapper.Verify(rm => rm.Map(_httpContext.Request, _httpContext.Items.DownstreamRoute()), Times.Once);
    }

    private void ThenTheDownstreamRequestIsStored()
    {
        _httpContext.Items.DownstreamRequest().ShouldNotBeNull();
    }

    private void ThenTheDownstreamRequestIsNotStored()
    {
        _httpContext.Items.DownstreamRequest().ShouldBeNull();
    }

    private void ThenAPipelineErrorIsStored()
    {
        _httpContext.Items.Errors().Count.ShouldBe(1);
        _httpContext.Items.Errors().First().ShouldBeOfType<UnmappableRequestError>();
        _httpContext.Items.Errors().First().Message.ShouldBe($"Error when parsing incoming request, exception: {_testException}");
    }

    private void ThenTheNextMiddlewareIsInvoked()
    {
        _next.Verify(n => n(_httpContext), Times.Once);
    }

    private void ThenTheNextMiddlewareIsNotInvoked()
    {
        _next.Verify(n => n(It.IsAny<HttpContext>()), Times.Never);
    }
}
