using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.QualityOfService;

namespace Ocelot.UnitTests.QualityOfService;

public class QualityOfServiceFactoryTests
{
    private QualityOfServiceFactory _factory;
    private ServiceCollection _services;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IHttpContextAccessor> _contextAccessor;

    public QualityOfServiceFactoryTests()
    {
        _services = new ServiceCollection();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _loggerFactory.Setup(x => x.CreateLogger<QualityOfServiceFactory>())
            .Returns(_logger.Object);
        _loggerFactory.Setup(x => x.CreateLogger<CircuitBreakerDelegatingHandler>())
            .Returns(_logger.Object);
        _contextAccessor = new Mock<IHttpContextAccessor>();
        var provider = _services.BuildServiceProvider(true);
        _factory = new QualityOfServiceFactory(provider, _contextAccessor.Object, _loggerFactory.Object);
    }

    [Fact]
    public void Get_NoQosOptions_ReturnedNoQosDelegatingHandler()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(new()) // UseQos -> false
            .Build();

        // Act
        var handler = _factory.Get(route);

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<NoQosDelegatingHandler>(handler);
    }

    [Fact]
    public void Get_ExternalLibRegisteredQosHandlerInDI_ReturnedCustomQosDelegatingHandler()
    {
        // Arrange
        _services = new ServiceCollection();

        static DelegatingHandler CreateCustomQosDelegatingHandler(DownstreamRoute a, IHttpContextAccessor b, IOcelotLoggerFactory c)
            => new FakeDelegatingHandler();

        _services.AddSingleton<QosDelegatingHandlerDelegate>(CreateCustomQosDelegatingHandler);
        var provider = _services.BuildServiceProvider(true);
        _factory = new QualityOfServiceFactory(provider, _contextAccessor.Object, _loggerFactory.Object);
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(new(1, 1)) // UseQos -> true
            .Build();

        // Act
        var handler = _factory.Get(route);

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<FakeDelegatingHandler>(handler);
    }

    [Fact]
    public void Get_NoDelegateRegistered_ReturnsNoQosDelegatingHandlerAndSetsError()
    {
        // Arrange: no QosDelegatingHandlerDelegate registered, but QoS options are set
        _services = new ServiceCollection(); // empty services
        var provider = _services.BuildServiceProvider(true);
        _factory = new QualityOfServiceFactory(provider, _contextAccessor.Object, _loggerFactory.Object);

        var mockContext = new Mock<HttpContext>();
        var mockItems = new Mock<IDictionary<object, object>>();
        mockContext.Setup(c => c.Items).Returns(mockItems.Object);
        _contextAccessor.Setup(a => a.HttpContext).Returns(mockContext.Object);

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(new(2, 1000)) // UseQos -> true
            .Build();

        // Act
        var handler = _factory.Get(route);

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<NoQosDelegatingHandler>(handler);
        _logger.Verify(l => l.LogCritical(It.IsAny<Func<string>>(), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public void Get_BuiltInDelegateWithCircuitBreakerOptions_ReturnsCircuitBreakerHandler()
    {
        // Arrange: register built-in QosDelegatingHandler (as AddOcelot().AddQualityOfService() does)
        _services = new ServiceCollection();
        _services.AddSingleton<QosDelegatingHandlerDelegate>(QosDelegatingHandler.Create);
        var provider = _services.BuildServiceProvider(true);
        _factory = new QualityOfServiceFactory(provider, _contextAccessor.Object, _loggerFactory.Object);

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(new(2, 1000)) // MinimumThroughput + BreakDuration -> CircuitBreakerDelegatingHandler
            .Build();

        // Act
        var handler = _factory.Get(route);

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<CircuitBreakerDelegatingHandler>(handler);
    }

    [Fact]
    public void Get_BuiltInDelegateWithTimeoutOnly_ReturnsCircuitBreakerHandler()
    {
        // Arrange: register built-in QosDelegatingHandler; route only has Timeout (no circuit breaker opts)
        _services = new ServiceCollection();
        _services.AddSingleton<QosDelegatingHandlerDelegate>(QosDelegatingHandler.Create);
        var provider = _services.BuildServiceProvider(true);
        _factory = new QualityOfServiceFactory(provider, _contextAccessor.Object, _loggerFactory.Object);

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(new(timeout: 1000)) // UseQos=true via Timeout only
            .Build();

        // Act
        var handler = _factory.Get(route);

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<CircuitBreakerDelegatingHandler>(handler);
    }

    [Fact]
    public void Get_FinderReturnsNull_ReturnsNoQosDelegatingHandlerAndSetsError()
    {
        // Arrange: delegate always returns null
        _services = new ServiceCollection();
        _services.AddSingleton<QosDelegatingHandlerDelegate>((route, accessor, loggerFactory) => null!);
        var provider = _services.BuildServiceProvider(true);
        _factory = new QualityOfServiceFactory(provider, _contextAccessor.Object, _loggerFactory.Object);

        var mockContext = new Mock<HttpContext>();
        var mockItems = new Mock<IDictionary<object, object>>();
        mockContext.Setup(c => c.Items).Returns(mockItems.Object);
        _contextAccessor.Setup(a => a.HttpContext).Returns(mockContext.Object);

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(new(2, 1000))
            .Build();

        // Act
        var handler = _factory.Get(route);

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<NoQosDelegatingHandler>(handler);
        _logger.Verify(l => l.LogCritical(It.IsAny<Func<string>>(), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert: no exception thrown on double-dispose
        _factory.Dispose();
        _factory.Dispose();
    }

    private class FakeDelegatingHandler : DelegatingHandler
    { }
}

