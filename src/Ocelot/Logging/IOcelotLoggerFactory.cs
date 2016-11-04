using System;
using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Logging
{
    public interface IOcelotLoggerFactory
    {
        IOcelotLogger CreateLogger<T>();
    }

    public class AspDotNetLoggerFactory : IOcelotLoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRequestScopedDataRepository _scopedDataRepository;

        public AspDotNetLoggerFactory(ILoggerFactory loggerFactory, IRequestScopedDataRepository scopedDataRepository)
        {
            _loggerFactory = loggerFactory;
            _scopedDataRepository = scopedDataRepository;
        }

        public IOcelotLogger CreateLogger<T>()
        {
            var logger = _loggerFactory.CreateLogger<T>();
            return new AspDotNetLogger(logger, _scopedDataRepository);
        }
    }

    public interface IOcelotLogger
    {
        void LogDebug(string message, params object[] args);
        void LogError(string message, Exception exception);
    }

    public class AspDotNetLogger : IOcelotLogger
    {
        private readonly ILogger _logger;
        private readonly IRequestScopedDataRepository _scopedDataRepository;

        public AspDotNetLogger(ILogger logger, IRequestScopedDataRepository scopedDataRepository)
        {
            _logger = logger;
            _scopedDataRepository = scopedDataRepository;
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(GetMessageWithRequestId(message), args);
        }

        public void LogError(string message, Exception exception)
        {
            _logger.LogError(GetMessageWithRequestId(message), exception);
        }

        private string GetMessageWithRequestId(string message)
        {
            var requestId = _scopedDataRepository.Get<string>("RequestId");

            return requestId.IsError 
                ? $"{message} : RequestId: Error" 
                : $"{message} : RequestId: {requestId.Data}";
        }
    }
}
