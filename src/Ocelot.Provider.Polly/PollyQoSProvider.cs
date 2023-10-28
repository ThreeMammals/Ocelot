using System.Net;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public class PollyQoSProvider : IPollyQoSProvider<HttpResponseMessage>
{
    private readonly Dictionary<string, CircuitBreaker<HttpResponseMessage>> _circuitBreakers = new();
    private readonly object _lockObject = new();
    private readonly IOcelotLogger _logger;

    private readonly HashSet<HttpStatusCode> _serverErrorCodes = new()
    {
        HttpStatusCode.InternalServerError,
        HttpStatusCode.NotImplemented,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.HttpVersionNotSupported,
        HttpStatusCode.VariantAlsoNegotiates,
        HttpStatusCode.InsufficientStorage,
        HttpStatusCode.LoopDetected,
    };

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

    public CircuitBreaker<HttpResponseMessage> GetCircuitBreaker(DownstreamRoute route)
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

    private CircuitBreaker<HttpResponseMessage> CircuitBreakerFactory(DownstreamRoute route)
    {
        AsyncCircuitBreakerPolicy<HttpResponseMessage> exceptionsAllowedBeforeBreakingPolicy = null;
        if (route.QosOptions.ExceptionsAllowedBeforeBreaking > 0)
        {
            var info = $"Route: {GetRouteName(route)}; Breaker logging in {nameof(PollyQoSProvider)}: ";

            exceptionsAllowedBeforeBreakingPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => _serverErrorCodes.Contains(r.StatusCode))
                .Or<TimeoutRejectedException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(route.QosOptions.ExceptionsAllowedBeforeBreaking,
                    TimeSpan.FromMilliseconds(route.QosOptions.DurationOfBreak), (ex, breakDelay) =>
                        _logger.LogError(info + $"Breaking the circuit for {breakDelay.TotalMilliseconds} ms!",
                            ex.Exception), () =>
                        _logger.LogDebug(info + "Call OK! Closed the circuit again."), () =>
                        _logger.LogDebug(info + "Half-open; Next call is a trial."));
        }

        _ = Enum.TryParse(route.QosOptions.TimeoutStrategy, out TimeoutStrategy strategy);
        var timeoutPolicy =
            Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(route.QosOptions.TimeoutValue),
                strategy);

        return new CircuitBreaker<HttpResponseMessage>(exceptionsAllowedBeforeBreakingPolicy, timeoutPolicy);
    }
}
