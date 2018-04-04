using Ocelot.Logging;
using Moq;
using TestStack.BDDfy;
using Shouldly;
using Butterfly.Client.Tracing;
using Ocelot.Requester;
using Xunit;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace Ocelot.UnitTests.Logging
{
    public class OcelotDiagnosticListenerTests
    {
        private OcelotDiagnosticListener _listener;
        private Mock<IOcelotLoggerFactory> _factory;
        private Mock<IOcelotLogger> _logger;
        private IServiceTracer _tracer;
        private DownstreamContext _downstreamContext;
        private string _name;

        public OcelotDiagnosticListenerTests()
        {
            _factory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _tracer = new FakeServiceTracer();
            _factory.Setup(x => x.CreateLogger<OcelotDiagnosticListener>()).Returns(_logger.Object);
            _listener = new OcelotDiagnosticListener(_factory.Object, _tracer);
        }

        [Fact]
        public void should_trace_ocelot_middleware_started()
        {
            GivenAMiddlewareName();
            GivenAContext();
            WhenOcelotMiddlewareStartedCalled();
            ThenTheOcelotStartedTraceIs();
        }

        [Fact]
        public void should_trace_ocelot_middleware_finished()
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void should_trace_ocelot_middleware_exception()
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void should_trace_middleware_started()
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void should_trace_middleware_finished()
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void should_trace_middleware_exception()
        {
            throw new System.NotImplementedException();
        }

        private void WhenOcelotMiddlewareStartedCalled()
        {
            _listener.OcelotMiddlewareStarted(_downstreamContext, _name);
        }

        private void GivenAContext()
        {
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
        }

        private void GivenAMiddlewareName()
        {
            _name = "name";
        }

        private void ThenTheOcelotStartedTraceIs()
        {
            _logger.Verify(
                x => x.LogTrace("Ocelot.MiddlewareStarted: {name}; {Path}", _name, _downstreamContext.HttpContext.Request.Path));
        }
    }
}