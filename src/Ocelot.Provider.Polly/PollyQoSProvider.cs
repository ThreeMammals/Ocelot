using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;

namespace Ocelot.Provider.Polly;

public class PollyQoSProvider : IPollyQoSProvider<HttpResponseMessage>
{
    private readonly Dictionary<string, PollyPolicyWrapper<HttpResponseMessage>> _policyWrappers = new();
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
        => string.IsNullOrWhiteSpace(route.ServiceName)
            ? route.UpstreamPathTemplate?.Template ?? route.DownstreamPathTemplate?.Value ?? string.Empty
            : route.ServiceName;

    public PollyPolicyWrapper<HttpResponseMessage> GetPollyPolicyWrapper(DownstreamRoute route)
    {
        lock (_lockObject)
        {
            var currentRouteName = GetRouteName(route);
            if (!_policyWrappers.ContainsKey(currentRouteName))
            {
                _policyWrappers.Add(currentRouteName, PollyPolicyWrapperFactory(route));
            }

            return _policyWrappers[currentRouteName];
        }
    }

    private PollyPolicyWrapper<HttpResponseMessage> PollyPolicyWrapperFactory(DownstreamRoute route)
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
                    durationOfBreak: TimeSpan.FromMilliseconds(route.QosOptions.DurationOfBreak),
                    onBreak: (ex, breakDelay) => _logger.LogError(info + $"Breaking the circuit for {breakDelay.TotalMilliseconds} ms!", ex.Exception),
                    onReset: () => _logger.LogDebug(info + "Call OK! Closed the circuit again."),
                    onHalfOpen: () => _logger.LogDebug(info + "Half-open; Next call is a trial."));
        }

        var timeoutPolicy = Policy
            .TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromMilliseconds(route.QosOptions.TimeoutValue), 
                TimeoutStrategy.Pessimistic);

        return new PollyPolicyWrapper<HttpResponseMessage>(exceptionsAllowedBeforeBreakingPolicy, timeoutPolicy);
    }
}
