namespace Ocelot.UnitTests.Authorization
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Authorisation;
    using Ocelot.Authorisation.Middleware;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;

    public class AuthorisationMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly Mock<IClaimsAuthoriser> _authService;
        private readonly Mock<IScopesAuthoriser> _authScopesService;
        private OkResponse<DownstreamRoute> _downstreamRoute;

        public AuthorisationMiddlewareTests()
        {
            _authService = new Mock<IClaimsAuthoriser>();
            _authScopesService = new Mock<IScopesAuthoriser>();

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_call_authorisation_service()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<PlaceholderNameAndValue>(), 
                new ReRouteBuilder()
                    .WithIsAuthorised(true)
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
                .And(x => x.GivenTheAuthServiceReturns(new OkResponse<bool>(true)))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAuthServiceIsCalledCorrectly())
                .BDDfy();
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_authService.Object);
            services.AddSingleton(_authScopesService.Object);
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseAuthorisationMiddleware();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            ScopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void GivenTheAuthServiceReturns(Response<bool> expected)
        {
            _authService
                .Setup(x => x.Authorise(It.IsAny<ClaimsPrincipal>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(expected);
        }

        private void ThenTheAuthServiceIsCalledCorrectly()
        {
            _authService
                .Verify(x => x.Authorise(It.IsAny<ClaimsPrincipal>(),
                It.IsAny<Dictionary<string, string>>()), Times.Once);
        }
    }
}
