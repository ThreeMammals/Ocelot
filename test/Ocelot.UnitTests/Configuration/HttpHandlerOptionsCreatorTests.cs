using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Configuration;

public class HttpHandlerOptionsCreatorTests : UnitTest
{
    private HttpHandlerOptionsCreator _creator;
    private FileRoute _fileRoute;
    private HttpHandlerOptions _httpHandlerOptions;
    private readonly Mock<IOcelotTracer> _ocTracer = new();

    public HttpHandlerOptionsCreatorTests()
    {
        _creator = new(_ocTracer.Object);
    }

    [Fact]
    public void Should_not_use_tracing_if_fake_tracer_registered()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new FileHttpHandlerOptions
            {
                UseTracing = true,
            },
        };
        var expectedOptions = new HttpHandlerOptions(false, false, false, false, int.MaxValue, DefaultPooledConnectionLifeTime);

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    [Fact]
    public void Should_use_tracing_if_real_tracer_registered()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new FileHttpHandlerOptions
            {
                UseTracing = true,
            },
        };
        var expectedOptions = new HttpHandlerOptions(false, false, true, false, int.MaxValue, DefaultPooledConnectionLifeTime);
        GivenARealTracer();

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    [Fact]
    public void Should_create_options_with_useCookie_false_and_allowAutoRedirect_true_as_default()
    {
        // Arrange
        _fileRoute = new FileRoute();
        var expectedOptions = new HttpHandlerOptions(false, false, false, false, int.MaxValue, DefaultPooledConnectionLifeTime);

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    [Fact]
    public void Should_create_options_with_specified_useCookie_and_allowAutoRedirect()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new FileHttpHandlerOptions
            {
                AllowAutoRedirect = false,
                UseCookieContainer = false,
                UseTracing = false,
            },
        };
        var expectedOptions = new HttpHandlerOptions(false, false, false, false, int.MaxValue, DefaultPooledConnectionLifeTime);

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    [Fact]
    public void Should_create_options_with_useproxy_true_as_default()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new FileHttpHandlerOptions(),
        };
        var expectedOptions = new HttpHandlerOptions(false, false, false, false, int.MaxValue, DefaultPooledConnectionLifeTime);

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    [Fact]
    public void Should_create_options_with_specified_useproxy()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new FileHttpHandlerOptions
            {
                UseProxy = true,
            },
        };
        var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime);

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    [Fact]
    public void Should_create_options_with_specified_MaxConnectionsPerServer()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new FileHttpHandlerOptions
            {
                MaxConnectionsPerServer = 10,
            },
        };

        var expectedOptions = new HttpHandlerOptions(false, false, false, false, 10, DefaultPooledConnectionLifeTime);

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    [Fact]
    public void Should_create_options_fixing_specified_MaxConnectionsPerServer_range()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new FileHttpHandlerOptions
            {
                MaxConnectionsPerServer = -1,
            },
        };

        var expectedOptions = new HttpHandlerOptions(false, false, false, false, int.MaxValue, DefaultPooledConnectionLifeTime);

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    [Fact]
    public void Should_create_options_fixing_specified_MaxConnectionsPerServer_range_when_zero()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new FileHttpHandlerOptions
            {
                MaxConnectionsPerServer = 0,
            },
        };
        var expectedOptions = new HttpHandlerOptions(false, false, false, false, int.MaxValue, DefaultPooledConnectionLifeTime);

        // Act
        _httpHandlerOptions = _creator.Create(_fileRoute.HttpHandlerOptions);

        // Assert
        ThenTheFollowingOptionsReturned(expectedOptions);
    }

    private void ThenTheFollowingOptionsReturned(HttpHandlerOptions expected)
    {
        _httpHandlerOptions.ShouldNotBeNull();
        _httpHandlerOptions.AllowAutoRedirect.ShouldBe(expected.AllowAutoRedirect);
        _httpHandlerOptions.UseCookieContainer.ShouldBe(expected.UseCookieContainer);
        _httpHandlerOptions.UseTracing.ShouldBe(expected.UseTracing);
        _httpHandlerOptions.UseProxy.ShouldBe(expected.UseProxy);
        _httpHandlerOptions.MaxConnectionsPerServer.ShouldBe(expected.MaxConnectionsPerServer);
    }

    private void GivenARealTracer()
    {
        _creator = new HttpHandlerOptionsCreator(new FakeTracer());
    }

    /// <summary>
    /// 120 seconds.
    /// </summary>
    private static TimeSpan DefaultPooledConnectionLifeTime => TimeSpan.FromSeconds(HttpHandlerOptions.DefaultPooledConnectionLifetimeSeconds);

    private class FakeTracer : IOcelotTracer
    {
        public void Event(HttpContext httpContext, string @event) => throw new NotImplementedException();

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Action<string> addTraceIdToRepo,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> baseSendAsync,
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
