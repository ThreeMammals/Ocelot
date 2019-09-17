using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.UnitTests.Responder;
using System.Collections.Generic;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class OcelotMiddlewareTests
    {
        private Mock<IOcelotLogger> _logger;
        private FakeMiddleware _middleware;
        private List<Error> _errors;

        public OcelotMiddlewareTests()
        {
            _errors = new List<Error>();
            _logger = new Mock<IOcelotLogger>();
            _middleware = new FakeMiddleware(_logger.Object);
        }

        [Fact]
        public void should_log_error()
        {
            this.Given(x => GivenAnError(new AnyError()))
                .When(x => WhenISetTheError())
                .Then(x => ThenTheErrorIsLogged(1))
                .BDDfy();
        }

        [Fact]
        public void should_log_errors()
        {
            this.Given(x => GivenAnError(new AnyError()))
                .And(x => GivenAnError(new AnyError()))
                .When(x => WhenISetTheErrors())
                .Then(x => ThenTheErrorIsLogged(2))
                .BDDfy();
        }

        private void WhenISetTheErrors()
        {
            _middleware.SetPipelineError(new DownstreamContext(new DefaultHttpContext()), _errors);
        }

        private void ThenTheErrorIsLogged(int times)
        {
            _logger.Verify(x => x.LogWarning("blahh"), Times.Exactly(times));
        }

        private void WhenISetTheError()
        {
            _middleware.SetPipelineError(new DownstreamContext(new DefaultHttpContext()), _errors[0]);
        }

        private void GivenAnError(Error error)
        {
            _errors.Add(error);
        }
    }

    public class FakeMiddleware : OcelotMiddleware
    {
        public FakeMiddleware(IOcelotLogger logger)
            : base(logger)
        {
        }
    }
}
