using Ocelot.Configuration;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Authentication
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Authentication.Middleware;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class AuthenticationMiddlewareTests
    {
        private OkResponse<DownstreamRoute> _downstreamRoute;
        private AuthenticationMiddleware _middleware;
        private Mock<IOcelotLoggerFactory> _factory;
        private Mock<IOcelotLogger> _logger;
        private OcelotRequestDelegate _next;
        private DownstreamContext _downstreamContext;

        public AuthenticationMiddlewareTests()
        {
            _factory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _factory.Setup(x => x.CreateLogger<AuthenticationMiddleware>()).Returns(_logger.Object);
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
        }

        [Fact]
        public void should_call_next_middleware_if_route_is_not_authenticated()
        {
            this.Given(x => GivenTheDownStreamRouteIs(
                    new DownstreamReRouteBuilder().WithUpstreamHttpMethod(new List<string> { "Get" }).Build()))
                .And(x => GivenTheTestServerPipelineIsConfigured())
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheUserIsAuthenticated())
                .BDDfy();
        }

        private  void WhenICallTheMiddleware()
        {
            _next = async (context) => {
                byte[] byteArray = Encoding.ASCII.GetBytes("The user is authenticated");
                MemoryStream stream = new MemoryStream(byteArray);
                context.HttpContext.Response.Body = stream;
            };
            _middleware = new AuthenticationMiddleware(_next, _factory.Object);
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheTestServerPipelineIsConfigured()
        {
            _next = async (context) => {
                byte[] byteArray = Encoding.ASCII.GetBytes("The user is authenticated");
                MemoryStream stream = new MemoryStream(byteArray);
                context.HttpContext.Response.Body = stream;
            };
        }

        private void ThenTheUserIsAuthenticated()
        {
            var content = _downstreamContext.HttpContext.Response.Body.AsString(); 
            content.ShouldBe("The user is authenticated");
        }

        private void GivenTheDownStreamRouteIs(DownstreamReRoute downstreamRoute)
        {
            _downstreamContext.DownstreamReRoute = downstreamRoute;
        }
    }

    public static class StreamExtensions
    {
        public static string AsString(this Stream stream)
        {
            using(var reader = new StreamReader(stream))
            {
                string text = reader.ReadToEnd();
                return text;
            };
        }
    }
}
