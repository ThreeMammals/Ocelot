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
            _logger.LogTrace("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", requestId, previousRequestId, message);
        }

        public void LogDebug(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogDebug("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", requestId, previousRequestId, message);
        }

        public void LogInformation(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogInformation("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", requestId, previousRequestId, message);
        }

        public void LogWarning(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogWarning("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", requestId, previousRequestId, message);
        }

        public void LogError(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogError("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}, exception: {exception}", requestId, previousRequestId, message, exception);
        }

        public void LogCritical(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogCritical("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}, exception: {exception}", requestId, previousRequestId, message, exception);
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
