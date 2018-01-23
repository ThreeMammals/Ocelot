using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Logging
{
    public class AspDotNetLogger : IOcelotLogger
    {
        private readonly ILogger _logger;
        private readonly IRequestScopedDataRepository _scopedDataRepository;

        public string Name { get; }

        public AspDotNetLogger(ILogger logger, IRequestScopedDataRepository scopedDataRepository, string typeName)
        {
            Name = typeName;
            _logger = logger;
            _scopedDataRepository = scopedDataRepository;
        }

        public void LogTrace(string message, params object[] args)
        {            
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogTrace("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message},", requestId, previousRequestId, new FormattedLogValues(message, args).ToString());
        }

        public void LogDebug(string message, params object[] args)
        {            
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogDebug("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message},", requestId, previousRequestId, new FormattedLogValues(message, args).ToString());
        }

        public void LogInformation(string message, params object[] args)
        {            
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogInformation("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message},", requestId, previousRequestId, new FormattedLogValues(message, args).ToString());
        }

        public void LogError(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogError("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}, exception: {exception}", requestId, previousRequestId, message, exception);
        }

        public void LogError(string message, params object[] args)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogError("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", requestId, previousRequestId, new FormattedLogValues(message, args).ToString());
        }

        public void LogCritical(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();
            _logger.LogError("requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}", requestId, previousRequestId, message);
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