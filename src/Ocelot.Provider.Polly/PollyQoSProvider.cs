using System;
using System.Net.Http;

using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;

using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly
{
    public class PollyQoSProvider : IPollyQoSProvider
    {
        public PollyQoSProvider(DownstreamRoute route, IOcelotLoggerFactory loggerFactory)
        {
            AsyncCircuitBreakerPolicy circuitBreakerPolicy;
            var logger = loggerFactory.CreateLogger<PollyQoSProvider>();

            _ = Enum.TryParse(route.QosOptions.TimeoutStrategy, out TimeoutStrategy strategy);

            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(route.QosOptions.TimeoutValue), strategy);

            if (route.QosOptions.ExceptionsAllowedBeforeBreaking > 0)
            {
                circuitBreakerPolicy = Policy
                    .Handle<HttpRequestException>()
                    .Or<TimeoutRejectedException>()
                    .Or<TimeoutException>()
                    .CircuitBreakerAsync(
                        exceptionsAllowedBeforeBreaking: route.QosOptions.ExceptionsAllowedBeforeBreaking,
                        durationOfBreak: TimeSpan.FromMilliseconds(route.QosOptions.DurationOfBreak),
                        onBreak: (ex, breakDelay) =>
                        {
                            logger.LogError(
                                ".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!", ex);
                        },
                        onReset: () =>
                        {
                            logger.LogDebug(".Breaker logging: Call ok! Closed the circuit again.");
                        },
                        onHalfOpen: () =>
                        {
                            logger.LogDebug(".Breaker logging: Half-open; next call is a trial.");
                        }
                    );
            }
            else
            {
                circuitBreakerPolicy = null;
            }

            CircuitBreaker = new CircuitBreaker(circuitBreakerPolicy, timeoutPolicy);
        }

        [Obsolete("do not use: it does nothing")]
        public PollyQoSProvider(AsyncCircuitBreakerPolicy circuitBreakerPolicy, AsyncTimeoutPolicy timeoutPolicy, IOcelotLogger logger)
        {
        }

        public CircuitBreaker CircuitBreaker { get; }
    }
}
