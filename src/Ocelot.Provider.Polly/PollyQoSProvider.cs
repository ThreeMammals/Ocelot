namespace Ocelot.Provider.Polly
{
    using System;
    using System.Net.Http;
    using global::Polly;
    using global::Polly.CircuitBreaker;
    using global::Polly.Timeout;
    using Ocelot.Configuration;
    using Ocelot.Logging;

    public class PollyQoSProvider
    {
        private readonly CircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly TimeoutPolicy _timeoutPolicy;
        private readonly IOcelotLogger _logger;

        public PollyQoSProvider(DownstreamReRoute reRoute, IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PollyQoSProvider>();

            Enum.TryParse(reRoute.QosOptions.TimeoutStrategy, out TimeoutStrategy strategy);

            _timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(reRoute.QosOptions.TimeoutValue), strategy);

            _circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: reRoute.QosOptions.ExceptionsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromMilliseconds(reRoute.QosOptions.DurationOfBreak),
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

            CircuitBreaker = new CircuitBreaker(_circuitBreakerPolicy, _timeoutPolicy);
        }

        public CircuitBreaker CircuitBreaker { get; }
    }
}
