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
            _logger.LogTrace(GetMessageWithOcelotRequestId(message), args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(GetMessageWithOcelotRequestId(message), args);
        }
        public void LogError(string message, Exception exception)
        {
            _logger.LogError(GetMessageWithOcelotRequestId(message), exception);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(GetMessageWithOcelotRequestId(message), args);
        }

        private string GetMessageWithOcelotRequestId(string message)
        {
            var requestId = _scopedDataRepository.Get<string>("RequestId");

            if (requestId == null || requestId.IsError)
            {
                return $"{message} : OcelotRequestId - not set";
            }

            return $"{message} : OcelotRequestId - {requestId.Data}";
        }
    }
}