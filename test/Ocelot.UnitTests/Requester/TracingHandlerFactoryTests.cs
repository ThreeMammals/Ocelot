using System;
using Butterfly.Client.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Requester;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class TracingHandlerFactoryTests
    {
        private TracingHandlerFactory _factory;
        private Mock<IServiceTracer> _tracer;
        private IServiceCollection _serviceCollection;
        private IServiceProvider _serviceProvider;
        private Mock<IRequestScopedDataRepository> _repo;

        public TracingHandlerFactoryTests()
        {
            _tracer = new Mock<IServiceTracer>();
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton<IServiceTracer>(_tracer.Object);
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
