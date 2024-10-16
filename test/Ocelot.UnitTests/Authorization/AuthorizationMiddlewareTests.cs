using Microsoft.AspNetCore.Http;
using Ocelot.Authorization;
using Ocelot.Authorization.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.Authorization
{
    public class AuthorizationMiddlewareTests : UnitTest
    {
        private readonly Mock<IClaimsAuthorizer> _authService;
        private readonly Mock<IScopesAuthorizer> _authScopesService;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly AuthorizationMiddleware _middleware;
        private readonly RequestDelegate _next;
        private readonly HttpContext _httpContext;

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
        public void should_call_authorization_service()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new List<PlaceholderNameAndValue>(),
                new DownstreamRouteBuilder()
                    .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().Build())
                    .WithIsAuthorized(true)
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build()))
                .And(x => x.GivenTheAuthServiceReturns(new OkResponse<bool>(true)))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAuthServiceIsCalledCorrectly())
                .BDDfy();
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
}
