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

    private class FakeDelegatingHandler : DelegatingHandler
    { }
}
