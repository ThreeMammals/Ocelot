using System;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Requester.QoS
{
    public class PollyQoSProvider : IQoSProvider
    {
        private readonly CircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly TimeoutPolicy _timeoutPolicy;
        private readonly IOcelotLogger _logger;
        private readonly CircuitBreaker _circuitBreaker;

        public PollyQoSProvider(ReRoute reRoute, IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PollyQoSProvider>();

            _timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(reRoute.QosOptionsOptions.TimeoutValue), reRoute.QosOptionsOptions.TimeoutStrategy);

            _circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: reRoute.QosOptionsOptions.ExceptionsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromMilliseconds(reRoute.QosOptionsOptions.DurationOfBreak),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogError(
                            ".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!", ex);
                    },
                    onReset: () =>
                    {
                        _logger.LogDebug(".Breaker logging: Call ok! Closed the circuit again.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogDebug(".Breaker logging: Half-open; next call is a trial.");
                    }
                );

            _circuitBreaker = new CircuitBreaker(_circuitBreakerPolicy, _timeoutPolicy);
        }

        public CircuitBreaker CircuitBreaker => _circuitBreaker;
    }
}