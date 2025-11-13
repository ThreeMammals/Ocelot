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
    private readonly Mock<IClaimsAuthorizer> _claimsAuthorizer;
    private readonly Mock<IScopesAuthorizer> _scopesAuthorizer;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly AuthorizationMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public AuthorizationMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _claimsAuthorizer = new Mock<IClaimsAuthorizer>();
        _scopesAuthorizer = new Mock<IScopesAuthorizer>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<AuthorizationMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new AuthorizationMiddleware(_next, _claimsAuthorizer.Object, _scopesAuthorizer.Object, _loggerFactory.Object);
    }

    [Fact]
    [Trait("Feat", "100")] // https://github.com/ThreeMammals/Ocelot/issues/100
    [Trait("PR", "104")] // https://github.com/ThreeMammals/Ocelot/pull/104
    [Trait("Release", "1.4.5")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.4.5
    public async Task Should_call_scopes_authorizer_when_route_is_authenticated()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().Build())
            .WithUpstreamHttpMethod([HttpMethods.Get])
            .WithAuthenticationOptions(new(new("authScheme")))
            .Build();
        GivenTheDownStreamRouteIs(new(), route);
        GivenScopesAuthorizerReturns(new OkResponse<bool>(true));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenScopesAuthorizerIsCalled();
    }

    [Fact]
    [Trait("Release", "1.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
    public async Task Should_call_authorization_service()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().Build())
            .WithUpstreamHttpMethod([HttpMethods.Get])
            /*.WithAuthenticationOptions(new(new("authScheme")))*/
            .WithRouteClaimsRequirement(new() { { "k", "v" } })
            .Build();
        GivenTheDownStreamRouteIs(new(), route);
        GivenClaimsAuthorizerReturns(new OkResponse<bool>(true));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenClaimsAuthorizerIsCalled();
    }

    private void GivenTheDownStreamRouteIs(List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, DownstreamRoute downstreamRoute)
    {
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(templatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute);
    }

    private void GivenScopesAuthorizerReturns(Response<bool> expected) => _scopesAuthorizer
            .Setup(x => x.Authorize(It.IsAny<ClaimsPrincipal>(), It.IsAny<List<string>>()))
            .Returns(expected);

    private void GivenClaimsAuthorizerReturns(Response<bool> expected) => _claimsAuthorizer
            .Setup(x => x.Authorize(It.IsAny<ClaimsPrincipal>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<List<PlaceholderNameAndValue>>()))
            .Returns(expected);

    private void ThenScopesAuthorizerIsCalled(Func<Times> times = null)
        => _scopesAuthorizer.Verify(
            x => x.Authorize(It.IsAny<ClaimsPrincipal>(), It.IsAny<List<string>>()),
            times ?? Times.Once);
    private void ThenClaimsAuthorizerIsCalled(Func<Times> times = null)
        => _claimsAuthorizer.Verify(
            x => x.Authorize(It.IsAny<ClaimsPrincipal>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<List<PlaceholderNameAndValue>>()),
            times ?? Times.Once);
}
