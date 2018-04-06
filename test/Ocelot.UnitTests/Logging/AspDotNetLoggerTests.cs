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
            _logger = new AspDotNetLogger(_coreLogger.Object, _repo.Object);
        }

        [Fact]
        public void should_log_trace()
        {
            var a = "tom";
            var b = "laura";
            
            _logger.LogTrace($"a message from {a} to {b}");

            ThenLevelIsLogged("requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura", LogLevel.Trace);
        }

        [Fact]
        public void should_log_info()
        {
            var a = "tom";
            var b = "laura";
            
            _logger.LogInformation($"a message from {a} to {b}");

            ThenLevelIsLogged("requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura", LogLevel.Information);
        }


        [Fact]
        public void should_log_warning()
        {
            var a = "tom";
            var b = "laura";
            
            _logger.LogWarning($"a message from {a} to {b}");

            ThenLevelIsLogged("requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura", LogLevel.Warning);
        }

        [Fact]
        public void should_log_error()
        {
            var a = "tom";
            var b = "laura";
            var ex = new Exception("oh no");
            
            _logger.LogError($"a message from {a} to {b}", ex);

            ThenLevelIsLogged("requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura, exception: System.Exception: oh no", LogLevel.Error);
        }

        [Fact]
        public void should_log_critical()
        {
            var a = "tom";
            var b = "laura";
            var ex = new Exception("oh no");
            
            _logger.LogCritical($"a message from {a} to {b}", ex);

            ThenLevelIsLogged("requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura, exception: System.Exception: oh no", LogLevel.Critical);
        }

        private void ThenLevelIsLogged(string expected, LogLevel expectedLogLevel)
        {
            _coreLogger.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<object>(o => o.ToString() == expected), 
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }
    }
}
