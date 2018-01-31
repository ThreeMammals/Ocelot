namespace Ocelot.UnitTests.Authentication
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Authentication.Middleware;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class AuthenticationMiddlewareTests : ServerHostedMiddlewareTest
    {
        private OkResponse<DownstreamRoute> _downstreamRoute;

        public AuthenticationMiddlewareTests()
        {
            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_call_next_middleware_if_route_is_not_authenticated()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRoute(
                    new List<PlaceholderNameAndValue>(), 
                    new ReRouteBuilder().WithUpstreamHttpMethod(new List<string> { "Get" }).Build())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheUserIsAuthenticated())
                .BDDfy();
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseAuthenticationMiddleware();

            app.Run(async x =>
            {
                await x.Response.WriteAsync("The user is authenticated");
            });
        }

        private void ThenTheUserIsAuthenticated()
        {
            var content = ResponseMessage.Content.ReadAsStringAsync().Result;
            content.ShouldBe("The user is authenticated");
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            ScopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }
    }
}
