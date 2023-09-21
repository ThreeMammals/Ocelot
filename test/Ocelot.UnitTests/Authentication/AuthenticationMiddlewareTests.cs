using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using TestStack.BDDfy;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Ocelot.UnitTests.Authentication
{
    public class AuthenticationMiddlewareTests
    {
        private readonly Mock<IAuthenticationService> _authentication;

        private readonly Mock<IOcelotLoggerFactory> _factory;

        private readonly HttpContext _httpContext;

        private readonly Mock<IOcelotLogger> _logger;

        private Ocelot.Authentication.Middleware.AuthenticationMiddleware _middleware;

        private RequestDelegate _next;

        private readonly Mock<IServiceProvider> _serviceProvider;

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
            _factory.Setup(x => x.CreateLogger<Ocelot.Authentication.Middleware.AuthenticationMiddleware>()).Returns(_logger.Object);
        }

        [Fact]
        public void should_call_next_middleware_if_route_is_not_authenticated()
        {
            this
                .Given(x => GivenTheDownStreamRouteIs(
                    new DownstreamRouteBuilder().WithUpstreamHttpMethod(new List<string> { "Get" }).Build()
                ))
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheUserIsAuthenticated())
                .BDDfy();
        }

        [Fact]
        public void should_call_next_middleware_if_route_is_using_options_method()
        {
            this
                .Given(x => GivenTheDownStreamRouteIs(
                    new DownstreamRouteBuilder()
                    .WithUpstreamHttpMethod(new List<string> { "Options" })
                    .WithIsAuthenticated(true)
                    .Build()
                ))
                .And(x => GivenTheRequestIsUsingOptionsMethod())
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheUserIsAuthenticated())
                .BDDfy();
        }

        [InlineData(false)]
        [InlineData(true)]
        [Theory]
        public void should_call_next_middleware_if_route_is_using_several_options_authentication_providers(bool isAuthenticationProviderKeys)
        {
            this
                .Given(x => GivenTheDownStreamRouteIs(
                    new DownstreamRouteBuilder()
                    .WithAuthenticationOptions(new Ocelot.Configuration.AuthenticationOptions(
                        null,
                        !isAuthenticationProviderKeys ? "Test" : null,
                        isAuthenticationProviderKeys ? new[] { string.Empty, "Test" } : null
                    ))
                    .WithIsAuthenticated(true)
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
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
            this
                .Given(x => GivenTheDownStreamRouteIs(
                    new DownstreamRouteBuilder()
                    .WithAuthenticationOptions(new Ocelot.Configuration.AuthenticationOptions(
                        null,
                        "Test",
                        new[] { "Test #1", string.Empty }
                    ))
                    .WithIsAuthenticated(true)
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
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

        private void GivenTheAuthenticationThrowsException()
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
            _middleware = new Ocelot.Authentication.Middleware.AuthenticationMiddleware(_next, _factory.Object);
            _middleware.Invoke(_httpContext).GetAwaiter().GetResult();
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

        private void GivenTheRequestIsUsingOptionsMethod()
        {
            _httpContext.Request.Method = "OPTIONS";
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

        private void WhenICallTheMiddleware()
        {
            _next = (context) =>
            {
                byte[] byteArray = Encoding.ASCII.GetBytes("The user is authenticated");
                var stream = new MemoryStream(byteArray);

                _httpContext.Response.Body = stream;
                return Task.CompletedTask;
            };
            _middleware = new Ocelot.Authentication.Middleware.AuthenticationMiddleware(_next, _factory.Object);
            _middleware.Invoke(_httpContext).GetAwaiter().GetResult();
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
