using System;
using IButterflyLogger = Butterfly.Client.Logging.ILogger;
using Microsoft.Extensions.Logging;

namespace Butterfly.Client.AspNetCore
{
    public class ButterflyLogger : IButterflyLogger
    {
        private readonly ILogger _logger;

        public ButterflyLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void Error(string message, Exception exception)
        {
            _logger.LogError(exception, message);
        }

        public void Info(string message)
        {
            _logger.LogInformation(message);
        }
    }
}