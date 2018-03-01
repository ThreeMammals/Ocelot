using Butterfly.Client.Tracing;
using Moq;
using Ocelot.Requester;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class TracingHandlerFactoryTests
    {
        private TracingHandlerFactory _factory;
        private Mock<IServiceTracer> _tracer;

        public TracingHandlerFactoryTests()
        {
            _tracer = new Mock<IServiceTracer>();
            _factory = new TracingHandlerFactory(_tracer.Object);
        }

        [Fact]
        public void should_return()
        {
            var handler = _factory.Get();
            handler.ShouldBeOfType<OcelotHttpTracingHandler>();
        }
    }
}