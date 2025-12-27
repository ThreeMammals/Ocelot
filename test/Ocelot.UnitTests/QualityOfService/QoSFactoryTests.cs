using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.QualityOfService;

namespace Ocelot.UnitTests.QualityOfService;

public class QoSFactoryTests
{
    private QoSFactory _factory;
    private ServiceCollection _services;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IHttpContextAccessor> _contextAccessor;

    public QoSFactoryTests()
    {
        _services = new ServiceCollection();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _contextAccessor = new Mock<IHttpContextAccessor>();
        var provider = _services.BuildServiceProvider(true);
        _factory = new QoSFactory(provider, _contextAccessor.Object, _loggerFactory.Object);
    }

    [Fact]
    public void Should_return_error()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder().Build();

        // Act
        var handler = _factory.Get(downstreamRoute);

        // Assert
        handler.IsError.ShouldBeTrue();
        handler.Errors[0].ShouldBeOfType<UnableToFindQoSProviderError>();
    }

    [Fact]
    public void Should_return_handler()
    {
        // Arrange
        _services = new ServiceCollection();

        static DelegatingHandler QosDelegatingHandlerDelegate(DownstreamRoute a, IHttpContextAccessor b, IOcelotLoggerFactory c) => new FakeDelegatingHandler();
        _services.AddSingleton<QosDelegatingHandlerDelegate>(QosDelegatingHandlerDelegate);
        var provider = _services.BuildServiceProvider(true);
        _factory = new QoSFactory(provider, _contextAccessor.Object, _loggerFactory.Object);
        var downstreamRoute = new DownstreamRouteBuilder().Build();

        // Act
        var handler = _factory.Get(downstreamRoute);

        // Assert
        handler.IsError.ShouldBeFalse();
        handler.Data.ShouldBeOfType<FakeDelegatingHandler>();
    }

    private class FakeDelegatingHandler : DelegatingHandler
    {
    }
}
