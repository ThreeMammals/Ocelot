using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using System;

namespace Ocelot.Logging
{
    public class AspDotNetLogger : IOcelotLogger
    {
        private readonly ILogger _logger;
        private readonly IRequestScopedDataRepository _scopedDataRepository;

        public AspDotNetLogger(ILogger logger, IRequestScopedDataRepository scopedDataRepository)
        {
            _logger = logger;
            _scopedDataRepository = scopedDataRepository;
        }

        public void LogTrace(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.Log(LogLevel.Trace, default(EventId), $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", null, null);
        }

        public void LogDebug(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.Log(LogLevel.Debug, default(EventId), $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", null, null);
        }

        public void LogInformation(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();

            _logger.Log(LogLevel.Information, default(EventId), $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", null, null);
        }

        public void LogWarning(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            //_logger.LogWarning("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", requestId, previousRequestId, message);
            _logger.Log(LogLevel.Warning, default(EventId), $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", null, null);
        }

        public void LogError(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.Log(LogLevel.Error,default(EventId), $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}, exception: {exception}", exception, null);
        }

        public void LogCritical(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.Log(LogLevel.Critical, default(EventId), $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}, exception: {exception}",exception, null);
        }

        private string GetOcelotRequestId()
        {
            var requestId = _scopedDataRepository.Get<string>("RequestId");

            if (requestId == null || requestId.IsError)
            {
                return "no request id";
            }

            return requestId.Data;
        }

        private string GetOcelotPreviousRequestId()
        {
            var requestId = _scopedDataRepository.Get<string>("PreviousRequestId");

            if (requestId == null || requestId.IsError)
            {
                return "no previous request id";
            }

            return requestId.Data;
        }
    }
}
