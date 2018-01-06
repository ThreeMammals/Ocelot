using System;
using Microsoft.Extensions.Logging;
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
            _logger.LogTrace("requestId: {requestId}, message: {message},", requestId, message, args);
        }

        public void LogDebug(string message, params object[] args)
        {            
            var requestId = GetOcelotRequestId();
            _logger.LogDebug("requestId: {requestId}, message: {message},", requestId, message, args);
        }

        public void LogInformation(string message, params object[] args)
        {            
            var requestId = GetOcelotRequestId();
            _logger.LogInformation("requestId: {requestId}, message: {message},", requestId, message, args);
        }

        public void LogError(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            _logger.LogError("requestId: {requestId}, message: {message}, exception: {exception}", requestId, message, exception);
        }

        public void LogError(string message, params object[] args)
        {
            var requestId = GetOcelotRequestId();
            _logger.LogError("requestId: {requestId}, message: {message}", requestId, message, args);
        }

        public void LogCritical(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            _logger.LogError("requestId: {requestId}, message: {message}", requestId, message);
        }

        private string GetOcelotRequestId()
        {
            var requestId = _scopedDataRepository.Get<string>("RequestId");

            if (requestId == null || requestId.IsError)
            {
                return $"Request Id not set";
            }

            return requestId.Data;
        }
    }
}