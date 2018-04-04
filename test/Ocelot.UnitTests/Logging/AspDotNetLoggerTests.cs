namespace Ocelot.UnitTests.Logging
{
    using Moq;
    using TestStack.BDDfy;
    using Shouldly;
    using Xunit;
    using Ocelot.Logging;
    using Microsoft.Extensions.Logging;
    using Ocelot.Infrastructure.RequestData;
    using System;
    using Microsoft.Extensions.Logging.Internal;

    public class AspDotNetLoggerTests
    {
        private Mock<ILogger<object>> _coreLogger;
        private AspDotNetLogger _logger; 
        private Mock<IRequestScopedDataRepository> _repo;

        public AspDotNetLoggerTests()
        {
            _coreLogger = new Mock<ILogger<object>>();
            _repo = new Mock<IRequestScopedDataRepository>();
            _logger = new AspDotNetLogger(_coreLogger.Object, _repo.Object, "AType");
        }

        [Fact]
        public void should_log_trace()
        {
            var tom = "tom";
            var laura = "laura";
            
            _logger.LogTrace("a message from {{tom}} to {{laura}}", tom, laura);

            ThenATraceIsLogged("requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura");
        }

        private void ThenATraceIsLogged(string expected)
        {
            _coreLogger.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<object>(o => o.ToString() == expected), 
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }
    }
}