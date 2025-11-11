using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using AuthenticationMiddleware = Ocelot.Authentication.Middleware.AuthenticationMiddleware;
using AuthenticationOptions = Ocelot.Configuration.AuthenticationOptions;

namespace Ocelot.UnitTests.Authentication;

public class AuthenticationMiddlewareTests : UnitTest
{
    private readonly Mock<IAuthenticationService> _authentication;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly DefaultHttpContext _httpContext;

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
        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(f => _logWarningMessages.Add(f.Invoke()));
    }

    [Fact]
    public void MiddlewareName_Cstor_ReturnsTypeName()
    {
        // Arrange
        var isNextCalled = false;
        _next = (context) =>
        {
            isNextCalled = true;
            return Task.CompletedTask;
        };
        _middleware = new AuthenticationMiddleware(_next, _factory.Object);
        var expected = _middleware.GetType().Name;

        // Act
        var actual = _middleware.MiddlewareName;

        // Assert
        Assert.False(isNextCalled);
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Should_call_next_middleware_if_route_is_not_authenticated()
    {
        // Arrange
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithUpstreamHttpMethod([HttpMethods.Get])
            .Build());

        // Act
        WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsAuthenticated();
    }

    [Fact]
    public void Should_call_next_middleware_if_route_is_using_options_method()
    {
        // Arrange
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithUpstreamHttpMethod([HttpMethods.Options])
            .WithIsAuthenticated(true)
            .Build());
        GivenTheRequestIsUsingMethod(HttpMethods.Options);

        // Act
        WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsAuthenticated();
    }

    [Fact]
    public void Should_call_next_middleware_if_route_is_using_several_options_authentication_providers()
    {
        // Arrange
        var multipleKeys = new string[] { string.Empty, "Fail", "Test" };
        var options = new AuthenticationOptions(null, multipleKeys);
        var methods = new List<string> { HttpMethods.Get };
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithAuthenticationOptions(options)
            .WithIsAuthenticated(true)
            .WithUpstreamHttpMethod(methods)
            .Build());
        GivenTheRequestIsUsingMethod(methods.First());
        GivenTheAuthenticationIsFail();
        GivenTheAuthenticationIsSuccess();
        GivenTheAuthenticationThrowsException();

        // Act
        WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsAuthenticated();
    }

    [Fact]
    public void Should_provide_backward_compatibility_if_route_has_several_options_authentication_providers()
    {
        // Arrange
        FileAuthenticationOptions opts = new()
        {
            AuthenticationProviderKey = "Test",
            AuthenticationProviderKeys = new[] { string.Empty, "Fail", "Test" },
        };
        AuthenticationOptions options = new(opts);
        var methods = new List<string> { HttpMethods.Get };
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithAuthenticationOptions(options)
            .WithIsAuthenticated(true)
            .WithUpstreamHttpMethod(methods)
            .Build());
        GivenTheRequestIsUsingMethod(methods.First());
        GivenTheAuthenticationIsFail();
        GivenTheAuthenticationIsSuccess();
        GivenTheAuthenticationThrowsException();

        // Act
        WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsAuthenticated();
    }

    [Fact]
    public void Should_not_call_next_middleware_and_return_no_result_if_all_multiple_keys_were_failed()
    {
        // Arrange
        var options = new AuthenticationOptions(null,
            new[] { string.Empty, "Fail", "Fail", "UnknownScheme" });
        var methods = new List<string> { HttpMethods.Get };
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithAuthenticationOptions(options)
            .WithIsAuthenticated(true)
            .WithUpstreamHttpMethod(methods)
            .Build());
        GivenTheRequestIsUsingMethod(methods.First());
        GivenTheAuthenticationIsFail();
        GivenTheAuthenticationIsSuccess();

        // Act
        WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsNotAuthenticated();
        _httpContext.User.Identity.IsAuthenticated.ShouldBeFalse();
        _logWarningMessages.Count.ShouldBe(1);
        _logWarningMessages.First().ShouldStartWith("Client has NOT been authenticated for path");
        _httpContext.Items.Errors().First().ShouldBeOfType<UnauthenticatedError>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Should_not_call_next_middleware_and_return_no_result_if_providers_keys_are_empty(int keysCount)
    {
        // Arrange
        var emptyKeys = new string[keysCount];
        for (int i = 0; i < emptyKeys.Length; i++)
        {
            emptyKeys[i] = i % 2 == 0 ? null : string.Empty;
        }

        var optionsWithEmptyKeys = new AuthenticationOptions(null, emptyKeys);
        var methods = new List<string> { "Get" };
        var route = new DownstreamRouteBuilder()
            .WithAuthenticationOptions(optionsWithEmptyKeys)
            .WithIsAuthenticated(true)
            .WithUpstreamHttpMethod(methods)
            .WithDownstreamPathTemplate("/" + nameof(Should_not_call_next_middleware_and_return_no_result_if_providers_keys_are_empty))
            .Build();
        GivenTheDownStreamRouteIs(route);
        GivenTheRequestIsUsingMethod(methods.First());

        // Act
        WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsNotAuthenticated();
        _httpContext.User.Identity.IsAuthenticated.ShouldBeFalse();
        _logWarningMessages.Count.ShouldBe(2);
        _logWarningMessages[0].ShouldBe("Unable to authenticate the client for route '/Should_not_call_next_middleware_and_return_no_result_if_providers_keys_are_empty' due to empty AuthenticationProviderKeys, even though AuthenticationOptions are defined.");
        _logWarningMessages[1].ShouldBe("Client has NOT been authenticated for path '' and pipeline error set. UnauthenticatedError: Request for authenticated route '' was unauthenticated;");
        _httpContext.Items.Errors().Count(e => e.GetType() == typeof(UnauthenticatedError)).ShouldBe(1);
    }

    private readonly List<string> _logWarningMessages = new();

    private void GivenTheAuthenticationIsFail()
    {
        _authentication
            .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.Is<string>(s => s.Equals("Fail"))))
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
    }

    private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
    {
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute);
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
