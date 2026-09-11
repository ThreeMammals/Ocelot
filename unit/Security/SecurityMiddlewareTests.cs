using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Security;
using System.Reflection;

namespace Ocelot.UnitTests.Security;

public sealed class SecurityMiddlewareTests : UnitTest
{
    private readonly List<Mock<ISecurityPolicy>> _securityPolicyList;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly SecurityMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly HttpContext _httpContext;

    public SecurityMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<SecurityMiddleware>()).Returns(_logger.Object);
        _securityPolicyList = new List<Mock<ISecurityPolicy>>
        {
            new(),
            new(),
        };
        _next = context => Task.CompletedTask;
        _middleware = new SecurityMiddleware(_next, _loggerFactory.Object, _securityPolicyList.Select(f => f.Object).ToList());
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
    }

    [Fact]
    public async Task Should_legal_request()
    {
        // Arrange
        GivenPassingSecurityVerification();

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert: security passed
        _httpContext.Items.Errors().Count.ShouldBe(0);
    }

    [Fact]
    public async Task Should_verification_failed_request()
    {
        // Arrange
        GivenNotPassingSecurityVerification();

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert: security not passed
        _httpContext.Items.Errors().Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Bug", "2403")] // https://github.com/ThreeMammals/Ocelot/issues/2403
    [Trait("PR", "2406")] // https://github.com/ThreeMammals/Ocelot/pull/2406
    public void HandleWebSocketErrors_Sets403ForWebSocketRequest()
    {
        // Arrange
        var context = CreateHttpContextWithWebSocket(isWebSocketRequest: true);
        var errors = new List<Error> { new SecurityError("WebSocket security violation") };

        // Act
        HandleWebSocketErrors(context, errors);

        // Assert
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    [Trait("Bug", "2403")] // https://github.com/ThreeMammals/Ocelot/issues/2403
    [Trait("PR", "2406")] // https://github.com/ThreeMammals/Ocelot/pull/2406
    public void HandleWebSocketErrors_DoesNotChangeStatusForNonWebSocketRequest()
    {
        // Arrange
        var context = CreateHttpContextWithWebSocket(isWebSocketRequest: false);
        context.Response.StatusCode = 200;
        var errors = new List<Error> { new SecurityError("Regular HTTP security violation") };

        // Act
        HandleWebSocketErrors(context, errors);

        // Assert
        Assert.Equal(200, context.Response.StatusCode); // unchanged
    }

    private static HttpContext CreateHttpContextWithWebSocket(bool isWebSocketRequest)
    {
        var context = new DefaultHttpContext();

        var webSocketFeature = new Mock<IHttpWebSocketFeature>();
        webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(isWebSocketRequest);

        context.Features.Set(webSocketFeature.Object);
        return context;
    }

    private void HandleWebSocketErrors(HttpContext context, List<Error> errors)
    {
        var method = typeof(SecurityMiddleware)
            .GetMethod(nameof(HandleWebSocketErrors), BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, [context, errors]);
    }

    private void GivenPassingSecurityVerification()
    {
        foreach (var item in _securityPolicyList)
        {
            Response response = new OkResponse();
            item.Setup(x => x.Security(_httpContext.Items.DownstreamRoute(), _httpContext)).Returns(response);
        }
    }

    private void GivenNotPassingSecurityVerification()
    {
        for (var i = 0; i < _securityPolicyList.Count; i++)
        {
            var item = _securityPolicyList[i];
            if (i == 0)
            {
                Error error = new UnauthenticatedError("Not passing security verification");
                Response response = new ErrorResponse(error);
                item.Setup(x => x.Security(_httpContext.Items.DownstreamRoute(), _httpContext)).Returns(response);
            }
            else
            {
                Response response = new OkResponse();
                item.Setup(x => x.Security(_httpContext.Items.DownstreamRoute(), _httpContext)).Returns(response);
            }
        }
    }
}
