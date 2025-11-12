using Microsoft.AspNetCore.Http;
using Ocelot.Authorization;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.Authorization;

public class AuthorizationMiddlewareTests : UnitTest
{
    private readonly Mock<IClaimsAuthorizer> _authService;
    private readonly Mock<IScopesAuthorizer> _authScopesService;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly AuthorizationMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public AuthorizationMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _authService = new Mock<IClaimsAuthorizer>();
        _authScopesService = new Mock<IScopesAuthorizer>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<AuthorizationMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new AuthorizationMiddleware(_next, _authService.Object, _authScopesService.Object, _loggerFactory.Object);
    }

    [Fact]
    public async Task Should_call_authorization_service()
    {
        // Arrange
        GivenTheDownStreamRouteIs(
            new List<PlaceholderNameAndValue>(),
            new DownstreamRouteBuilder()
                .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().Build())
                .WithUpstreamHttpMethod([HttpMethods.Get])
                /*.WithAuthenticationOptions(new(new("authScheme")))*/
                .WithRouteClaimsRequirement(new() { { "k", "v" } })
                .Build());
        GivenTheAuthServiceReturns(new OkResponse<bool>(true));

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheAuthServiceIsCalledCorrectly();
    }

    private async Task WhenICallTheMiddleware()
    {
        await _middleware.Invoke(_httpContext);
    }

    private void GivenTheDownStreamRouteIs(List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, DownstreamRoute downstreamRoute)
    {
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(templatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute);
    }

    private void GivenTheAuthServiceReturns(Response<bool> expected)
    {
        _authService
            .Setup(x => x.Authorize(
                       It.IsAny<ClaimsPrincipal>(),
                       It.IsAny<Dictionary<string, string>>(),
                       It.IsAny<List<PlaceholderNameAndValue>>()))
            .Returns(expected);
    }

    private void ThenTheAuthServiceIsCalledCorrectly()
    {
        _authService.Verify(
            x => x.Authorize(It.IsAny<ClaimsPrincipal>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<List<PlaceholderNameAndValue>>()),
            Times.Once);
    }
}
