using Butterfly.Client.Tracing;
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
        private Mock<IRequestScopedDataRepository> _repo;

        public TracingHandlerFactoryTests()
        {
            _tracer = new Mock<IServiceTracer>();
            _repo = new Mock<IRequestScopedDataRepository>();
            _factory = new TracingHandlerFactory(_tracer.Object, _repo.Object);
        }

        [Fact]
        public void should_return()
        {
            var handler = _factory.Get();
            handler.ShouldBeOfType<OcelotHttpTracingHandler>();
        }
    }
}