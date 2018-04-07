using Ocelot.Logging;
using Moq;
using TestStack.BDDfy;
using Butterfly.Client.Tracing;
using Ocelot.Requester;
using Xunit;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Http;
using System;

namespace Ocelot.UnitTests.Logging
{
    public class OcelotDiagnosticListenerTests
    {
        private readonly OcelotDiagnosticListener _listener;
        private Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IOcelotLogger> _logger;
        private IServiceTracer _tracer;
        private DownstreamContext _downstreamContext;
        private string _name;
        private Exception _exception;

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
            this.Given(_ => GivenAMiddlewareName())
                .And(_ => GivenAContext())
                .When(_ => WhenOcelotMiddlewareStartedCalled())
                .Then(_ => ThenTheLogIs($"Ocelot.MiddlewareStarted: {_name}; {_downstreamContext.HttpContext.Request.Path}"))
                .BDDfy();
        }

        [Fact]
        public void should_trace_ocelot_middleware_finished()
        {
            this.Given(_ => GivenAMiddlewareName())
                .And(_ => GivenAContext())
                .When(_ => WhenOcelotMiddlewareFinishedCalled())
                .Then(_ => ThenTheLogIs($"Ocelot.MiddlewareFinished: {_name}; {_downstreamContext.HttpContext.Request.Path}"))
                .BDDfy();
        }

        [Fact]
        public void should_trace_ocelot_middleware_exception()
        {
            this.Given(_ => GivenAMiddlewareName())
                .And(_ => GivenAContext())
                .And(_ => GivenAException(new Exception("oh no")))
                .When(_ => WhenOcelotMiddlewareExceptionCalled())
                .Then(_ => ThenTheLogIs($"Ocelot.MiddlewareException: {_name}; {_exception.Message};"))
                .BDDfy();
        }

       [Fact]
        public void should_trace_middleware_started()
        {
            this.Given(_ => GivenAMiddlewareName())
                .And(_ => GivenAContext())
                .When(_ => WhenMiddlewareStartedCalled())
                .Then(_ => ThenTheLogIs($"MiddlewareStarting: {_name}; {_downstreamContext.HttpContext.Request.Path}"))
                .BDDfy();
        }

        [Fact]
        public void should_trace_middleware_finished()
        {
            this.Given(_ => GivenAMiddlewareName())
                .And(_ => GivenAContext())
                .When(_ => WhenMiddlewareFinishedCalled())
                .Then(_ => ThenTheLogIs($"MiddlewareFinished: {_name}; {_downstreamContext.HttpContext.Response.StatusCode}"))
                .BDDfy();
        }

        [Fact]
        public void should_trace_middleware_exception()
        {
            this.Given(_ => GivenAMiddlewareName())
                .And(_ => GivenAContext())
                .And(_ => GivenAException(new Exception("oh no")))
                .When(_ => WhenMiddlewareExceptionCalled())
                .Then(_ => ThenTheLogIs($"MiddlewareException: {_name}; {_exception.Message};"))
                .BDDfy();
        }

        private void GivenAException(Exception exception)
        {
            _exception = exception;
        }

        private void WhenOcelotMiddlewareStartedCalled()
        {
            _listener.OcelotMiddlewareStarted(_downstreamContext, _name);
        }

        private void WhenOcelotMiddlewareFinishedCalled()
        {
            _listener.OcelotMiddlewareFinished(_downstreamContext, _name);
        }

        private void WhenOcelotMiddlewareExceptionCalled()
        {
            _listener.OcelotMiddlewareException(_exception, _downstreamContext, _name);
        }

        private void WhenMiddlewareStartedCalled()
        {
            _listener.OnMiddlewareStarting(_downstreamContext.HttpContext, _name);
        }

        private void WhenMiddlewareFinishedCalled()
        {
            _listener.OnMiddlewareFinished(_downstreamContext.HttpContext, _name);
        }

        private void WhenMiddlewareExceptionCalled()
        {
            _listener.OnMiddlewareException(_exception, _name);
        }

        private void GivenAContext()
        {
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
        }

        private void GivenAMiddlewareName()
        {
            _name = "name";
        }

        private void ThenTheLogIs(string expected)
        {
            _logger.Verify(
                x => x.LogTrace(expected));
        }
    }
}
