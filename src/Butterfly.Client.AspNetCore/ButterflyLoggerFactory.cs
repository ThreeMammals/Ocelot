using System;
using Microsoft.Extensions.Logging;

namespace Butterfly.Client.AspNetCore
{
    public class ButterflyLoggerFactory : Logging.ILoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public ButterflyLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public Logging.ILogger CreateLogger(Type type)
        {
            return new ButterflyLogger(_loggerFactory.CreateLogger(type));
        }
    }
}