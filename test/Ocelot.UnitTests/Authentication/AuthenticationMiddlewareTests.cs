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
using AuthenticationMiddleware = Ocelot.Authentication.AuthenticationMiddleware;
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
    private bool _isNextCalled;

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
        _logger.Setup(x => x.LogInformation(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(f => _logInformationMessages.Add(f.Invoke()));
        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(f => _logWarningMessages.Add(f.Invoke()));
    }

    [Fact]
    public void MiddlewareName_Cstor_ReturnsTypeName()
    {
        // Arrange
        _isNextCalled = false;
        _next = (context) =>
        {
            _isNextCalled = true;
            return Task.CompletedTask;
        };
        _middleware = new AuthenticationMiddleware(_next, _factory.Object);
        var expected = _middleware.GetType().Name;

        // Act
        var actual = _middleware.MiddlewareName;

        // Assert
        Assert.False(_isNextCalled);
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Should_call_next_middleware_if_route_is_not_authenticated()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithUpstreamHttpMethod([HttpMethods.Get])
            .WithAuthenticationOptions(new())
            .Build();
        GivenTheDownStreamRouteIs(route);

        // Act
        await WhenICallTheMiddleware(route.IsAuthenticated);

        // Assert
        ThenTheUserIsAuthenticated("The user is NOT authenticated");
    }

    [Fact]
    public async Task Should_call_next_middleware_if_route_is_using_options_method()
    {
        // Arrange
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithUpstreamHttpMethod([HttpMethods.Options])
            .WithAuthenticationOptions(new())
            .Build());
        GivenTheRequestIsUsingMethod(HttpMethods.Options);

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsAuthenticated();
    }

    [Fact]
    [Trait("Feat", "740")] // https://github.com/ThreeMammals/Ocelot/issues/740
    [Trait("Feat", "1580")] // https://github.com/ThreeMammals/Ocelot/issues/1580
    [Trait("PR", "1870")] // https://github.com/ThreeMammals/Ocelot/pull/1870
    public async Task Should_call_next_middleware_if_route_is_using_several_options_authentication_providers()
    {
        // Arrange
        var multipleKeys = new string[] { string.Empty, "Fail", "Test" };
        var options = new AuthenticationOptions(null, multipleKeys);
        var methods = new List<string> { HttpMethods.Get };
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithAuthenticationOptions(options)
            .WithUpstreamHttpMethod(methods)
            .Build());
        GivenTheRequestIsUsingMethod(methods.First());
        GivenTheAuthenticationIsFail();
        GivenTheAuthenticationIsSuccess();
        GivenTheAuthenticationThrowsException();

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsAuthenticated();
    }

    [Fact]
    [Trait("Feat", "740")] // https://github.com/ThreeMammals/Ocelot/issues/740
    [Trait("Feat", "1580")] // https://github.com/ThreeMammals/Ocelot/issues/1580
    [Trait("PR", "1870")] // https://github.com/ThreeMammals/Ocelot/pull/1870
    public async Task Should_provide_backward_compatibility_if_route_has_several_options_authentication_providers()
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
            .WithUpstreamHttpMethod(methods)
            .Build());
        GivenTheRequestIsUsingMethod(methods.First());
        GivenTheAuthenticationIsFail();
        GivenTheAuthenticationIsSuccess();
        GivenTheAuthenticationThrowsException();

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsAuthenticated();
    }

    [Fact]
    [Trait("Feat", "740")] // https://github.com/ThreeMammals/Ocelot/issues/740
    [Trait("Feat", "1580")] // https://github.com/ThreeMammals/Ocelot/issues/1580
    [Trait("PR", "1870")] // https://github.com/ThreeMammals/Ocelot/pull/1870
    public async Task Should_not_call_next_middleware_and_return_no_result_if_all_multiple_keys_were_failed()
    {
        // Arrange
        var options = new AuthenticationOptions(null,
            new[] { string.Empty, "Fail", "Fail", "UnknownScheme" });
        var methods = new List<string> { HttpMethods.Get };
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithAuthenticationOptions(options)
            .WithUpstreamHttpMethod(methods)
            .Build());
        GivenTheRequestIsUsingMethod(methods.First());
        GivenTheAuthenticationIsFail();
        GivenTheAuthenticationIsSuccess();

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsNotAuthenticated();
        _httpContext.User.Identity.IsAuthenticated.ShouldBeFalse();
        _logWarningMessages.Count.ShouldBe(1);
        _logWarningMessages.First().ShouldStartWith("Client has NOT been authenticated for path");
        _httpContext.Items.Errors().First().ShouldBeOfType<UnauthenticatedError>();
    }

    private void GivenHappyPath(bool isHappy = true, string userName = null)
    {
        var multipleKeys = new string[] { "Test" };
        var options = new AuthenticationOptions(null, multipleKeys);
        string[] methods = [HttpMethods.Get];
        GivenTheDownStreamRouteIs(new DownstreamRouteBuilder()
            .WithAuthenticationOptions(options)
            .WithUpstreamHttpMethod(methods)
            .Build());
        GivenTheRequestIsUsingMethod(methods[0]);
        GivenTheAuthenticationIsFail();
        GivenTheAuthenticationIsSuccess(isHappy, userName); // Identity.IsAuthenticated -> true by default
        GivenTheAuthenticationThrowsException();
    }

    [Fact]
    [Trait("Feat", "740")] // https://github.com/ThreeMammals/Ocelot/issues/740
    [Trait("Feat", "1580")] // https://github.com/ThreeMammals/Ocelot/issues/1580
    [Trait("PR", "1870")] // https://github.com/ThreeMammals/Ocelot/pull/1870
    public async Task Should_SetUnauthenticatedError_and_not_call_next_middleware_if_identity_IsNOT_authenticated()
    {
        // Arrange
        GivenHappyPath(false);// Identity.IsAuthenticated -> false

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsNotAuthenticated();
        Assert.False(_isNextCalled);
        _httpContext.User.Identity.IsAuthenticated.ShouldBeFalse();
        _logInformationMessages.Count.ShouldBe(1);
        _logWarningMessages.Count.ShouldBe(1);
        _logWarningMessages[0].ShouldBe("Client has NOT been authenticated for path '' and pipeline error set. UnauthenticatedError: Request for authenticated route '' was unauthenticated!;");
        _httpContext.Items.Errors().Count.ShouldBe(1);
        var e = _httpContext.Items.Errors()[0];
        Assert.IsType<UnauthenticatedError>(e);
        Assert.Equal("Request for authenticated route '' was unauthenticated!", e.Message);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("Igor", 1)]
    [Trait("Feat", "740")] // https://github.com/ThreeMammals/Ocelot/issues/740
    [Trait("Feat", "1580")] // https://github.com/ThreeMammals/Ocelot/issues/1580
    [Trait("PR", "1870")] // https://github.com/ThreeMammals/Ocelot/pull/1870
    public async Task SetUnauthenticatedError(string userName, int index)
    {
        // Arrange
        var warnings = new string[]
        {
            "Client has NOT been authenticated for path '' and pipeline error set. UnauthenticatedError: Request for authenticated route '' was unauthenticated!;",
            "Client has NOT been authenticated for path '' and pipeline error set. UnauthenticatedError: Request for authenticated route '' by 'Igor' was unauthenticated!;",
        };
        var messages = new string[]
        {
            "Request for authenticated route '' was unauthenticated!",
            "Request for authenticated route '' by 'Igor' was unauthenticated!",
        };
        GivenHappyPath(false, userName);// Identity.IsAuthenticated -> false

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsNotAuthenticated();
        Assert.False(_isNextCalled);
        Assert.Equal(warnings[index], _logWarningMessages[0]);
        var e = _httpContext.Items.Errors()[0];
        Assert.IsType<UnauthenticatedError>(e);
        Assert.Equal(messages[index], e.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [Trait("Feat", "740")] // https://github.com/ThreeMammals/Ocelot/issues/740
    [Trait("Feat", "1580")] // https://github.com/ThreeMammals/Ocelot/issues/1580
    [Trait("PR", "1870")] // https://github.com/ThreeMammals/Ocelot/pull/1870
    public async Task Should_not_call_next_middleware_and_return_no_result_if_providers_keys_are_empty(int keysCount)
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
            .WithUpstreamHttpMethod(methods)
            .WithDownstreamPathTemplate("/" + TestName())
            .Build();
        GivenTheDownStreamRouteIs(route);
        GivenTheRequestIsUsingMethod(methods.First());

        // Act
        await WhenICallTheMiddleware(route.IsAuthenticated);

        // Assert
        ThenTheUserIsAuthenticated("The user is NOT authenticated");
        _httpContext.User.Identity.IsAuthenticated.ShouldBeFalse();
        _logWarningMessages.Count.ShouldBe(0);
        _logInformationMessages.Count.ShouldBe(1);
        _logInformationMessages[0].ShouldBe("No authentication is required for the path '' in the route /Should_not_call_next_middleware_and_return_no_result_if_providers_keys_are_empty.");
        _httpContext.Items.Errors().Count(e => e.GetType() == typeof(UnauthenticatedError)).ShouldBe(0);
    }

    [Fact]
    [Trait("Feat", "740")] // https://github.com/ThreeMammals/Ocelot/issues/740
    [Trait("Feat", "1580")] // https://github.com/ThreeMammals/Ocelot/issues/1580
    [Trait("PR", "1870")] // https://github.com/ThreeMammals/Ocelot/pull/1870
    public async Task AuthenticateAsync_CatchException()
    {
        // Arrange
        GivenHappyPath(false);// Identity.IsAuthenticated -> false
        _authentication
            .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Throws<HttpContext, string, InvalidOperationException>((ctx, scheme) => new("Bad auth scheme -> " + scheme));

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheUserIsNotAuthenticated();
        Assert.False(_isNextCalled);
        Assert.False(_httpContext.User.Identity.IsAuthenticated);
        Assert.Equal(2, _logWarningMessages.Count);
        Assert.Equal("Unable to authenticate the client for route '?' using the Test authentication scheme due to error: Bad auth scheme -> Test", _logWarningMessages[0]);
        Assert.Equal("Client has NOT been authenticated for path '' and pipeline error set. UnauthenticatedError: Request for authenticated route '' was unauthenticated!;", _logWarningMessages[1]);
        var errors = _httpContext.Items.Errors();
        Assert.Equal(1, errors.Count);
        Assert.IsType<UnauthenticatedError>(errors[0]);
        Assert.Equal("Request for authenticated route '' was unauthenticated!", errors[0].Message);
    }

    private readonly List<string> _logInformationMessages = new();
    private readonly List<string> _logWarningMessages = new();

    private void GivenTheAuthenticationIsFail()
    {
        _authentication
            .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.Is<string>(s => s.Equals("Fail"))))
            .Returns(Task.FromResult(AuthenticateResult.Fail("The user is not authenticated.")));
    }

    private void GivenTheAuthenticationIsSuccess(bool isAuthenticated = true, string userName = null)
    {
        var principal = new Mock<ClaimsPrincipal>();
        var identity = new Mock<IIdentity>();
        identity.Setup(i => i.IsAuthenticated).Returns(isAuthenticated);
        identity.Setup(i => i.Name).Returns(userName ?? string.Empty);
        principal.Setup(p => p.Identity).Returns(identity.Object);
        _authentication
            .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.Is<string>(s => s.Equals("Test"))))
            .Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal.Object, "Test"))));
    }

    private void GivenTheAuthenticationThrowsException()
    {
        _authentication
            .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.Is<string>(scheme => string.Empty.Equals(scheme))))
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

    private void ThenTheUserIsAuthenticated(string expected = null)
    {
        var content = _httpContext.Response.Body.AsString();
        content.ShouldBe(expected ?? "The user is authenticated");
    }

    private void ThenTheUserIsNotAuthenticated(string expected = null)
    {
        var content = _httpContext.Response.Body.AsString();
        var errors = _httpContext.Items.Errors();

        content.ShouldBe(expected ?? string.Empty);
        errors.ShouldNotBeEmpty();
    }

    private Task WhenICallTheMiddleware(bool isAuthenticated = true)
    {
        _isNextCalled = false;
        _next = (context) =>
        {
            _isNextCalled = true;
            var not = !isAuthenticated ? " NOT" : string.Empty;
            byte[] byteArray = Encoding.ASCII.GetBytes($"The user is{not} authenticated");
            var stream = new MemoryStream(byteArray);
            _httpContext.Response.Body = stream;
            return Task.CompletedTask;
        };
        _middleware = new AuthenticationMiddleware(_next, _factory.Object);
        return _middleware.Invoke(_httpContext);
    }
}
