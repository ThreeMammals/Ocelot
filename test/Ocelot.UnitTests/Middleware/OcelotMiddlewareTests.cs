namespace Ocelot.UnitTests.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.Errors;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.UnitTests.Responder;
    using System.Collections.Generic;
    using TestStack.BDDfy;
    using Xunit;

    public class OcelotMiddlewareTests
    {
        private Mock<IOcelotLogger> _logger;
        private FakeMiddleware _middleware;
        private List<Error> _errors;
        private HttpContext httpContext;

        public OcelotMiddlewareTests()
        {
            httpContext = new DefaultHttpContext();
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
            httpContext.Items.SetErrors(_errors);
        }

        private void ThenTheErrorIsLogged(int times)
        {
            _logger.Verify(x => x.LogWarning("blahh"), Times.Exactly(times));
        }

        private void WhenISetTheError()
        {
            httpContext.Items.SetError(_errors[0]);
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
