using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    private IServiceProvider _serviceProvider;
    private readonly IServiceCollection _serviceCollection;

    public HttpHandlerOptionsCreatorTests()
    {
        _serviceCollection = new ServiceCollection();
        _serviceProvider = _serviceCollection.BuildServiceProvider(true);
        _creator = new HttpHandlerOptionsCreator(_serviceProvider);
    }

    [Fact]
    public void Should_not_use_tracing_if_fake_tracer_registered()
    {
        // Arrange
        _fileRoute = new FileRoute
        {
            HttpHandlerOptions = new()
            {
                UseTracing = true,
            },
        };
        var expectedOptions = new HttpHandlerOptions();

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
            HttpHandlerOptions = new()
            {
                UseTracing = true,
            },
        };
        var expectedOptions = new HttpHandlerOptions() { UseTracing = true };
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
        var expectedOptions = new HttpHandlerOptions() { UseCookieContainer = false };

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
            HttpHandlerOptions = new()
            {
                AllowAutoRedirect = false,
                UseCookieContainer = false,
                UseTracing = false,
            },
        };
        var expectedOptions = new HttpHandlerOptions();

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
            HttpHandlerOptions = new(),
        };
        var expectedOptions = new HttpHandlerOptions();

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
            HttpHandlerOptions = new()
            {
                UseProxy = true,
            },
        };
        var expectedOptions = new HttpHandlerOptions() { UseProxy = true };

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
            HttpHandlerOptions = new()
            {
                MaxConnectionsPerServer = 10,
            },
        };

        var expectedOptions = new HttpHandlerOptions() { MaxConnectionsPerServer = 10 };

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

        var expectedOptions = new HttpHandlerOptions() { MaxConnectionsPerServer = int.MaxValue };

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
        var expectedOptions = new HttpHandlerOptions() { MaxConnectionsPerServer = int.MaxValue };

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
        _serviceCollection.AddSingleton<IOcelotTracer, FakeTracer>();
        _serviceProvider = _serviceCollection.BuildServiceProvider(true);
        _creator = new HttpHandlerOptionsCreator(_serviceProvider);
    }

    private class FakeTracer : IOcelotTracer
    {
        public void Event(HttpContext httpContext, string @event) => throw new NotImplementedException();

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Action<string> addTraceIdToRepo,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> baseSendAsync,
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
