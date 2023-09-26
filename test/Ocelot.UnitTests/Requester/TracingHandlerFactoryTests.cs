using Microsoft.Extensions.DependencyInjection;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Requester;

namespace Ocelot.UnitTests.Requester
{
    public class TracingHandlerFactoryTests
    {
        private readonly TracingHandlerFactory _factory;
        private readonly Mock<ITracer> _tracer;
        private readonly IServiceCollection _serviceCollection;
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<IRequestScopedDataRepository> _repo;

        public TracingHandlerFactoryTests()
        {
            _tracer = new Mock<ITracer>();
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton(_tracer.Object);
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _repo = new Mock<IRequestScopedDataRepository>();
            _factory = new TracingHandlerFactory(_serviceProvider, _repo.Object);
        }

        [Fact]
        public void should_return()
        {
            var handler = _factory.Get();
            handler.ShouldBeOfType<OcelotHttpTracingHandler>();
        }
    }
}
