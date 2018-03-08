using Ocelot.Configuration;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Authentication
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Authentication.Middleware;
    using Ocelot.Configuration.Builder;
    using Ocelot.Logging;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class AuthenticationMiddlewareTests
    {
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

        private void WhenICallTheMiddleware()
        {
            _next = (context) => {
                byte[] byteArray = Encoding.ASCII.GetBytes("The user is authenticated");
                var stream = new MemoryStream(byteArray);
                context.HttpContext.Response.Body = stream;
                return Task.CompletedTask;
            };
            _middleware = new AuthenticationMiddleware(_next, _factory.Object);
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheTestServerPipelineIsConfigured()
        {
            _next = (context) => {
                byte[] byteArray = Encoding.ASCII.GetBytes("The user is authenticated");
                var stream = new MemoryStream(byteArray);
                context.HttpContext.Response.Body = stream;
                return Task.CompletedTask;
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
            }
        }
    }
}
