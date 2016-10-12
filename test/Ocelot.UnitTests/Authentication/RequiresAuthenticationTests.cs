namespace Ocelot.UnitTests.Authentication
{
    using System.Collections.Generic;
    using Library.Infrastructure.Authentication;
    using Library.Infrastructure.Configuration;
    using Library.Infrastructure.DownstreamRouteFinder;
    using Library.Infrastructure.Responses;
    using Library.Infrastructure.UrlMatcher;
    using Moq;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class RequiresAuthenticationTests
    {
        private readonly RouteRequiresAuthentication _routeRequiresAuthentication;
        private string _url;
        private readonly Mock<IOcelotConfiguration> _config;
        private Response<bool> _result;
        private string _httpMethod;

        public RequiresAuthenticationTests()
        {
            _config = new Mock<IOcelotConfiguration>();
            _routeRequiresAuthentication = new RouteRequiresAuthentication(_config.Object);            
        }

        [Fact]
        public void should_return_true_if_route_requires_authentication()
        {
            this.Given(x => x.GivenIHaveADownstreamUrl("http://www.bbc.co.uk"))
                .And(
                    x =>
                        x.GivenTheConfigurationForTheRouteIs(new ReRoute("http://www.bbc.co.uk", "/api/poo", "get",
                            "/api/poo$", true)))
                .When(x => x.WhenICheckToSeeIfTheRouteShouldBeAuthenticated())
                .Then(x => x.ThenTheResultIs(true))
                .BDDfy();
        }

        [Fact]
        public void should_return_false_if_route_requires_authentication()
        {
            this.Given(x => x.GivenIHaveADownstreamUrl("http://www.bbc.co.uk"))
               .And(
                   x =>
                       x.GivenTheConfigurationForTheRouteIs(new ReRoute("http://www.bbc.co.uk", "/api/poo", "get",
                           "/api/poo$", false)))
               .When(x => x.WhenICheckToSeeIfTheRouteShouldBeAuthenticated())
               .Then(x => x.ThenTheResultIs(false))
               .BDDfy();
        }

        [Fact]
        public void should_return_error_if_no_matching_config()
        {
            this.Given(x => x.GivenIHaveADownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheConfigurationForTheRouteIs(new ReRoute(string.Empty, string.Empty, string.Empty, string.Empty,false)))
               .When(x => x.WhenICheckToSeeIfTheRouteShouldBeAuthenticated())
               .Then(x => x.ThenAnErrorIsReturned())
               .BDDfy();
        }

        private void ThenAnErrorIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }

        public void GivenIHaveADownstreamUrl(string url)
        {
            _url = url;
        }

        private void GivenTheConfigurationForTheRouteIs(ReRoute reRoute)
        {
            _httpMethod = reRoute.UpstreamHttpMethod;

            _config
                .Setup(x => x.ReRoutes)
                .Returns(new List<ReRoute>
                {
                    reRoute
                });
        }

        private void WhenICheckToSeeIfTheRouteShouldBeAuthenticated()
        {
            _result = _routeRequiresAuthentication.IsAuthenticated(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), _url), _httpMethod);
        }

        private void ThenTheResultIs(bool expected)
        {
            _result.Data.ShouldBe(expected);
        }
    }
}
