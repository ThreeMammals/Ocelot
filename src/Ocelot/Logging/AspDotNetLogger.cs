using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using System;

namespace Ocelot.Logging
{
    public class AspDotNetLogger : IOcelotLogger
    {
        private readonly ILogger _logger;
        private readonly IRequestScopedDataRepository _scopedDataRepository;
        private readonly Func<string, Exception, string> _func;

        public AspDotNetLogger(ILogger logger, IRequestScopedDataRepository scopedDataRepository)
        {
            _logger = logger;
            _scopedDataRepository = scopedDataRepository;
            _func = (state, exception) => exception == null ? state : $"{state}, exception: {exception}";
        }

        public void LogTrace(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();

            var state = $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}";

            _logger.Log(LogLevel.Trace, default, state, null, _func);
        }

        public void LogDebug(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();

            var state = $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}";

            _logger.Log(LogLevel.Debug, default, state, null, _func);
        }

        public void LogInformation(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();

            var state = $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}";

            _logger.Log(LogLevel.Information, default, state, null, _func);
        }

        public void LogWarning(string message)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();

            var state = $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}";

            _logger.Log(LogLevel.Warning, default, state, null, _func);
        }

        public void LogError(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();

            var state = $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}";

            _logger.Log(LogLevel.Error, default, state, exception, _func);
        }

        public void LogCritical(string message, Exception exception)
        {
            var requestId = GetOcelotRequestId();
            var previousRequestId = GetOcelotPreviousRequestId();

            var state = $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: {message}";

            _logger.Log(LogLevel.Critical, default, state, exception, _func);
        }

        private string GetOcelotRequestId()
        {
            var requestId = _scopedDataRepository.Get<string>("RequestId");

            return requestId == null || requestId.IsError ? "no request id" : requestId.Data;
        }

        private string GetOcelotPreviousRequestId()
        {
            var requestId = _scopedDataRepository.Get<string>("PreviousRequestId");

            return requestId == null || requestId.IsError ? "no previous request id" : requestId.Data;
        }
    }
}
