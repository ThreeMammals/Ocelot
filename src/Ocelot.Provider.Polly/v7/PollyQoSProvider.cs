using Ocelot.Configuration;
using Ocelot.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly.v7;

/// <summary>Legacy QoS provider based on Polly v7.</summary>
/// <remarks>Use the <see cref="PollyQoSResiliencePipelineProvider"/> as a new QoS provider based on Polly v8.</remarks>
[Obsolete("Due to new v8 policy definition in Polly 8 (use PollyQoSResiliencePipelineProvider)")]
public class PollyQoSProvider : PollyQoSProviderBase, IPollyQoSProvider<HttpResponseMessage>
{
    private readonly Dictionary<string, PollyPolicyWrapper<HttpResponseMessage>> _policyWrappers = new();

    private readonly object _lockObject = new();
    private readonly IOcelotLogger _logger;

    // TODO: This should be configurable and available as global config parameter in ocelot.json
    public const int DefaultRequestTimeoutSeconds = 90;

    public PollyQoSProvider(IOcelotLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PollyQoSProvider>();
    }

    [Obsolete("Due to new v8 policy definition in Polly 8 (use GetResiliencePipeline in PollyQoSResiliencePipelineProvider)")]
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
                .HandleResult<HttpResponseMessage>(r => ServerErrorCodes.Contains(r.StatusCode))
                .Or<TimeoutRejectedException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(route.QosOptions.ExceptionsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromMilliseconds(route.QosOptions.DurationOfBreak),
                    onBreak: (ex, breakDelay) =>
                        _logger.LogError(info + $"Breaking the circuit for {breakDelay.TotalMilliseconds} ms!",
                            ex.Exception),
                    onReset: () => _logger.LogDebug(info + "Call OK! Closed the circuit again."),
                    onHalfOpen: () => _logger.LogDebug(info + "Half-open; Next call is a trial."));
        }

        // No default set for polly timeout at the minute.
        // Since a user could potentially set timeout value = 0, we need to handle this case.
        // TODO throw an exception if the user sets timeout value = 0 or at least return a warning
        // TODO the design in DelegatingHandlerHandlerFactory should be reviewed
        var timeoutPolicy = Policy
            .TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromMilliseconds(route.QosOptions.TimeoutValue),
                TimeoutStrategy.Pessimistic);

        return new PollyPolicyWrapper<HttpResponseMessage>(exceptionsAllowedBeforeBreakingPolicy, timeoutPolicy);
    }
}
