using Microsoft.Extensions.DependencyInjection;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Logging;

public class TracingHandlerFactoryTests
{
    private readonly TracingHandlerFactory _factory;
    private readonly Mock<IOcelotTracer> _tracer;
    private readonly IServiceCollection _serviceCollection;
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IRequestScopedDataRepository> _repo;

    public TracingHandlerFactoryTests()
    {
        _tracer = new Mock<IOcelotTracer>();
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddSingleton(_tracer.Object);
        _serviceProvider = _serviceCollection.BuildServiceProvider(true);
        _repo = new Mock<IRequestScopedDataRepository>();
        _factory = new TracingHandlerFactory(_serviceProvider, _repo.Object);
    }

    [Fact]
    public void Should_return()
    {
        // Arrange, Act
        var handler = _factory.Get();

        // Assert
        handler.ShouldBeOfType<OcelotHttpTracingHandler>();
    }
}
