using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly
{
    public class PollyQoSProvider : IPollyQoSProvider
    {
        public PollyQoSProvider(DownstreamRoute route, IOcelotLoggerFactory loggerFactory)
        {
            AsyncCircuitBreakerPolicy circuitBreakerPolicy = null;
            if (route.QosOptions.ExceptionsAllowedBeforeBreaking > 0)
            {
                var info = $"Route: {GetRouteName(route)}; Breaker logging in {nameof(PollyQoSProvider)}: ";
                var logger = loggerFactory.CreateLogger<PollyQoSProvider>();
                circuitBreakerPolicy = Policy
                    .Handle<HttpRequestException>()
                    .Or<TimeoutRejectedException>()
                    .Or<TimeoutException>()
                    .CircuitBreakerAsync(
                        exceptionsAllowedBeforeBreaking: route.QosOptions.ExceptionsAllowedBeforeBreaking,
                        durationOfBreak: TimeSpan.FromMilliseconds(route.QosOptions.DurationOfBreak),
                        onBreak: (ex, breakDelay) =>
                            logger.LogError(info + $"Breaking the circuit for {breakDelay.TotalMilliseconds} ms!", ex),
                        onReset: () =>
                            logger.LogDebug(info + "Call OK! Closed the circuit again."),
                        onHalfOpen: () =>
                            logger.LogDebug(info + "Half-open; Next call is a trial.")
                    );
            }

            _ = Enum.TryParse(route.QosOptions.TimeoutStrategy, out TimeoutStrategy strategy);
            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(route.QosOptions.TimeoutValue), strategy);
            CircuitBreaker = new CircuitBreaker(circuitBreakerPolicy, timeoutPolicy);
        }

        private const string ObsoleteConstructorMessage = $"Use the constructor {nameof(PollyQoSProvider)}({nameof(DownstreamRoute)} route, {nameof(IOcelotLoggerFactory)} loggerFactory)!";

        [Obsolete(ObsoleteConstructorMessage)]
        public PollyQoSProvider(AsyncCircuitBreakerPolicy circuitBreakerPolicy, AsyncTimeoutPolicy timeoutPolicy, IOcelotLogger logger)
        {
            throw new NotSupportedException(ObsoleteConstructorMessage);
        }

        public CircuitBreaker CircuitBreaker { get; }

        private static string GetRouteName(DownstreamRoute route)
            => string.IsNullOrWhiteSpace(route.ServiceName)
                ? route.UpstreamPathTemplate?.Template ?? route.DownstreamPathTemplate?.Value ?? string.Empty
                : route.ServiceName;
    }
}
