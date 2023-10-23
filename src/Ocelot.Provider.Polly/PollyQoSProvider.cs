using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public class PollyQoSProvider : IPollyQoSProvider
{
    private readonly Dictionary<string, CircuitBreaker> _circuitBreakers = new();
    private readonly object _lockObject = new();
    private readonly IOcelotLogger _logger;

    public PollyQoSProvider(IOcelotLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PollyQoSProvider>();
    }

    private static string GetRouteName(DownstreamRoute route)
    {
        return string.IsNullOrWhiteSpace(route.ServiceName)
            ? route.UpstreamPathTemplate?.Template ?? route.DownstreamPathTemplate?.Value ?? string.Empty
            : route.ServiceName;
    }

    public CircuitBreaker GetCircuitBreaker(DownstreamRoute route)
    {
        lock (_lockObject)
        {
            var currentRouteName = GetRouteName(route);
            if (!_circuitBreakers.ContainsKey(currentRouteName))
            {
                _circuitBreakers.Add(currentRouteName, CircuitBreakerFactory(route));
            }

            return _circuitBreakers[currentRouteName];
        }
    }

    private CircuitBreaker CircuitBreakerFactory(DownstreamRoute route)
    {
        AsyncCircuitBreakerPolicy circuitBreakerPolicy = null;
        if (route.QosOptions.ExceptionsAllowedBeforeBreaking > 0)
        {
            var info = $"Route: {GetRouteName(route)}; Breaker logging in {nameof(PollyQoSProvider)}: ";
            circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: route.QosOptions.ExceptionsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromMilliseconds(route.QosOptions.DurationOfBreak),
                    onBreak: (ex, breakDelay) =>
                        _logger.LogError(info + $"Breaking the circuit for {breakDelay.TotalMilliseconds} ms!", ex),
                    onReset: () =>
                        _logger.LogDebug(info + "Call OK! Closed the circuit again."),
                    onHalfOpen: () =>
                        _logger.LogDebug(info + "Half-open; Next call is a trial.")
            );
        }

        _ = Enum.TryParse(route.QosOptions.TimeoutStrategy, out TimeoutStrategy strategy);
        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(route.QosOptions.TimeoutValue), strategy);
        return new CircuitBreaker(circuitBreakerPolicy, timeoutPolicy);
    }
}
