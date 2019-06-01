using Ocelot.Middleware;

namespace Ocelot.UnitTests.Authorization
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Authorisation;
    using Ocelot.Authorisation.Middleware;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using TestStack.BDDfy;
    using Xunit;

    public class AuthorisationMiddlewareTests
    {
        private readonly Mock<IClaimsAuthoriser> _authService;
        private readonly Mock<IScopesAuthoriser> _authScopesService;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly AuthorisationMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public AuthorisationMiddlewareTests()
        {
            _authService = new Mock<IClaimsAuthoriser>();
            _authScopesService = new Mock<IScopesAuthoriser>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<AuthorisationMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _middleware = new AuthorisationMiddleware(_next, _authService.Object, _authScopesService.Object, _loggerFactory.Object);
        }

        [Fact]
        public void should_call_authorisation_service()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new List<PlaceholderNameAndValue>(),
                new DownstreamReRouteBuilder()
                    .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().Build())
                    .WithIsAuthorised(true)
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build()))
                .And(x => x.GivenTheAuthServiceReturns(new OkResponse<bool>(true)))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAuthServiceIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheDownStreamRouteIs(List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, DownstreamReRoute downstreamReRoute)
        {
            _downstreamContext.TemplatePlaceholderNameAndValues = templatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamReRoute;
        }

        private void GivenTheAuthServiceReturns(Response<bool> expected)
        {
            _authService
                .Setup(x => x.Authorise(
                           It.IsAny<ClaimsPrincipal>(),
                           It.IsAny<Dictionary<string, string>>(),
                           It.IsAny<List<PlaceholderNameAndValue>>()))
                .Returns(expected);
        }

        private void ThenTheAuthServiceIsCalledCorrectly()
        {
            _authService
                .Verify(x => x.Authorise(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<List<PlaceholderNameAndValue>>())
                        , Times.Once);
        }
    }
}
