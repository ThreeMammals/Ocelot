using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using AuthenticationMiddleware = Ocelot.Authentication.Middleware.AuthenticationMiddleware;
using AuthenticationOptions = Ocelot.Configuration.AuthenticationOptions;

namespace Ocelot.UnitTests.Authentication
{
    public class AuthenticationMiddlewareTests
    {
        private readonly Mock<IAuthenticationService> _authentication;
        private readonly Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly HttpContext _httpContext;

        private AuthenticationMiddleware _middleware;
        private RequestDelegate _next;

        public AuthenticationMiddlewareTests()
        {
            _authentication = new Mock<IAuthenticationService>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(_authentication.Object);
            _httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider.Object,
            };
            _factory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _factory.Setup(x => x.CreateLogger<AuthenticationMiddleware>()).Returns(_logger.Object);
        }

        [Fact]
        public void should_call_next_middleware_if_route_is_not_authenticated()
        {
            this.Given(x => GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
                    .WithUpstreamHttpMethod(new() { "Get" })
                    .Build()
                ))
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheUserIsAuthenticated())
                .BDDfy();
        }

        [Fact]
        public void should_call_next_middleware_if_route_is_using_options_method()
        {
            const string OPTIONS = "OPTIONS";
            this.Given(x => GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
                    .WithUpstreamHttpMethod(new() { OPTIONS })
                    .WithIsAuthenticated(true)
                    .Build()
                ))
                .And(x => GivenTheRequestIsUsingMethod(OPTIONS))
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheUserIsAuthenticated())
                .BDDfy();
        }

        [InlineData(false)]
        [InlineData(true)]
        [Theory]
        public void should_call_next_middleware_if_route_is_using_several_options_authentication_providers(bool isAuthenticationProviderKeys)
        {
            var options = new AuthenticationOptions(
                null,
                !isAuthenticationProviderKeys ? "Test" : null,
                isAuthenticationProviderKeys ? new[] { string.Empty, "Test" } : null
            );
            this.Given(x => GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
                    .WithAuthenticationOptions(options)
                    .WithIsAuthenticated(true)
                    .WithUpstreamHttpMethod(new() { "Get" })
                    .Build()
                ))
                .And(x => GivenTheRequestIsUsingGetMethod())
                .And(x => GivenTheAuthenticationIsFail())
                .And(x => GivenTheAuthenticationIsSuccess())
                .And(x => GivenTheAuthenticationThrowsException())
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheUserIsAuthenticated())
                .BDDfy();
        }

        [Fact]
        public void should_not_call_next_middleware_if_route_is_using_several_options_authentication_providers()
        {
            var options = new AuthenticationOptions(
                null,
                "Test",
                ["Test #1", string.Empty]
            );
            this.Given(x => GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
                    .WithAuthenticationOptions(options)
                    .WithIsAuthenticated(true)
                    .WithUpstreamHttpMethod(new() { "Get" })
                    .Build()
                ))
                .And(x => GivenTheRequestIsUsingGetMethod())
                .And(x => GivenTheAuthenticationIsFail())
                .And(x => GivenTheAuthenticationThrowsException())
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheUserIsNotAuthenticated())
                .BDDfy();
        }

        private void GivenTheAuthenticationIsFail()
        {
            _authentication
                .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .Returns(Task.FromResult(AuthenticateResult.Fail("The user is not authenticated.")));
        }

        private void GivenTheAuthenticationIsSuccess()
        {
            var principal = new Mock<ClaimsPrincipal>();
            var identity = new Mock<IIdentity>();

            identity.Setup(i => i.IsAuthenticated).Returns(true);
            principal.Setup(p => p.Identity).Returns(identity.Object);
            _authentication
                .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.Is<string>(s => s.Equals("Test"))))
                .Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal.Object, "Test"))));
        }

        private async void GivenTheAuthenticationThrowsException()
        {
            _authentication
                .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.Is<string>(s => string.Empty.Equals(s))))
                .Throws(new InvalidOperationException("Authentication provider key is empty."));
            _next = (context) =>
            {
                var byteArray = Encoding.ASCII.GetBytes("The user is authenticated");
                var stream = new MemoryStream(byteArray);
                _httpContext.Response.Body = stream;
                return Task.CompletedTask;
            };
            _middleware = new AuthenticationMiddleware(_next, _factory.Object);
            await _middleware.Invoke(_httpContext);
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _httpContext.Items.UpsertDownstreamRoute(downstreamRoute);
        }

        private void GivenTheRequestIsUsingGetMethod()
        {
            _httpContext.Request.Method = "GET";
            _next = (context) =>
            {
                var byteArray = Encoding.ASCII.GetBytes("The user is authenticated");
                var stream = new MemoryStream(byteArray);
                _httpContext.Response.Body = stream;
                return Task.CompletedTask;
            };
        }

        private void GivenTheRequestIsUsingMethod(string method)
        {
            _httpContext.Request.Method = method;
        }

        private void ThenTheUserIsAuthenticated()
        {
            var content = _httpContext.Response.Body.AsString();

            content.ShouldBe("The user is authenticated");
        }

        private void ThenTheUserIsNotAuthenticated()
        {
            var content = _httpContext.Response.Body.AsString();
            var errors = _httpContext.Items.Errors();

            content.ShouldBe(string.Empty);
            errors.ShouldNotBeEmpty();
        }

        private async void WhenICallTheMiddleware()
        {
            _next = (context) =>
            {
                byte[] byteArray = Encoding.ASCII.GetBytes("The user is authenticated");
                var stream = new MemoryStream(byteArray);

                _httpContext.Response.Body = stream;
                return Task.CompletedTask;
            };
            _middleware = new AuthenticationMiddleware(_next, _factory.Object);
            await _middleware.Invoke(_httpContext);
        }
    }

    public static class StreamExtensions
    {
        public static string AsString(this Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var text = reader.ReadToEnd();
                return text;
            }
        }
    }
}
