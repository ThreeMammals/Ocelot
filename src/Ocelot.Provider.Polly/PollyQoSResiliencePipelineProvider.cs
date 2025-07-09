using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;
using System.Linq;
using System.Net;

namespace Ocelot.Provider.Polly;

/// <summary>
/// Default provider for Polly V8 pipelines.
/// </summary>
public class PollyQoSResiliencePipelineProvider : IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
{
    private readonly ResiliencePipelineRegistry<OcelotResiliencePipelineKey> _registry;
    private readonly IOcelotLogger _logger;
    private readonly FileGlobalConfiguration _globalConfiguration;
    
    public PollyQoSResiliencePipelineProvider(
        IOcelotLoggerFactory loggerFactory,
        ResiliencePipelineRegistry<OcelotResiliencePipelineKey> registry,
        IOptions<FileGlobalConfiguration> global)
    {
        _logger = loggerFactory?.CreateLogger<PollyQoSResiliencePipelineProvider>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _globalConfiguration = global?.Value ?? throw new ArgumentNullException(nameof(global));
    }

    protected static readonly HashSet<HttpStatusCode> DefaultServerErrorCodes = new()
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

    protected virtual HashSet<HttpStatusCode> ServerErrorCodes { get; } = DefaultServerErrorCodes;
    protected virtual string GetRouteName(DownstreamRoute route) => route.Name();

    /// <summary>
    /// Gets Polly V8 resilience pipeline (applies QoS feature) for the route.
    /// </summary>
    /// <param name="route">The downstream route to apply the pipeline for.</param>
    /// <returns>A <see cref="ResiliencePipeline{T}"/> object where T is <see cref="HttpResponseMessage"/>.</returns>
    public ResiliencePipeline<HttpResponseMessage> GetResiliencePipeline(DownstreamRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        var globalQos = new QoSOptions(_globalConfiguration.QoSOptions);
        if (!route.QosOptions.UseQos && !globalQos.UseQos)
        {
            return ResiliencePipeline<HttpResponseMessage>.Empty; // shortcut -> No QoS
        }

        return _registry.GetOrAddPipeline<HttpResponseMessage>(
            key: new OcelotResiliencePipelineKey(GetRouteName(route)),
            configure: (builder) => ConfigureStrategies(builder, route));
    }

    protected virtual void ConfigureStrategies(ResiliencePipelineBuilder<HttpResponseMessage> builder, DownstreamRoute route)
    {
        ConfigureCircuitBreaker(builder, route);
        ConfigureTimeout(builder, route);
    }

    protected virtual string CircuitBreakerValidationMessage(DownstreamRoute route)
        => $"Route '{GetRouteName(route)}' has invalid {nameof(QoSOptions)} for Polly's Circuit Breaker strategy. Specifically, ";

    protected virtual bool IsConfigurationValidForCircuitBreaker(DownstreamRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(route.QosOptions);

        var qos = route.QosOptions;
        if (qos.ExceptionsAllowedBeforeBreaking <= 0)
        {
            _logger.LogError(
                () => CircuitBreakerValidationMessage(route) + $"the circuit breaker is disabled because the {nameof(qos.ExceptionsAllowedBeforeBreaking)} value ({qos.ExceptionsAllowedBeforeBreaking}) is either negative or zero.", null);
            return false;
        }

        List<Func<string>> warnings = new(), w = warnings;
        if (!qos.ExceptionsAllowedBeforeBreaking.IsValidMinimumThroughput())
        {
            string msg1() => $"{The(w, msg1)} {nameof(CircuitBreakerStrategy.MinimumThroughput)} value ({qos.ExceptionsAllowedBeforeBreaking}) is less than the required {nameof(CircuitBreakerStrategy.LowMinimumThroughput)} threshold ({CircuitBreakerStrategy.LowMinimumThroughput}). Therefore, increase {nameof(qos.ExceptionsAllowedBeforeBreaking)} to at least {CircuitBreakerStrategy.LowMinimumThroughput} or higher. Until then, the default value ({CircuitBreakerStrategy.DefaultMinimumThroughput}) will be substituted.";
            warnings.Add(msg1);
        }

        if (!qos.DurationOfBreak.IsValidBreakDuration())
        {
            string msg2() => $"{The(w, msg2)} {nameof(CircuitBreakerStrategy.BreakDuration)} value ({qos.DurationOfBreak}) is outside the valid range ({CircuitBreakerStrategy.LowBreakDuration} to {CircuitBreakerStrategy.HighBreakDuration} milliseconds). Therefore, ensure the value falls within this range; otherwise, the default value ({CircuitBreakerStrategy.DefaultBreakDuration}) will be substituted.";
            warnings.Add(msg2);
        }

        if (!qos.FailureRatio.IsValidFailureRatio())
        {
            string msg3() => $"{The(w, msg3)} {nameof(CircuitBreakerStrategy.FailureRatio)} value ({qos.FailureRatio}) is outside the valid range ({CircuitBreakerStrategy.LowFailureRatio} to {CircuitBreakerStrategy.HighFailureRatio}). Therefore, ensure the ratio falls within this range; otherwise, the default value ({CircuitBreakerStrategy.DefaultFailureRatio}) will be substituted.";
            warnings.Add(msg3);
        }

        if (!qos.SamplingDuration.IsValidSamplingDuration())
        {
            string msg4() => $"{The(w, msg4)} {nameof(CircuitBreakerStrategy.SamplingDuration)} value ({qos.SamplingDuration}) is outside the valid range ({CircuitBreakerStrategy.LowSamplingDuration} to {CircuitBreakerStrategy.HighSamplingDuration} milliseconds). Therefore, ensure the duration falls within this range; otherwise, the default value ({CircuitBreakerStrategy.DefaultSamplingDuration}) will be substituted.";
            warnings.Add(msg4);
        }

        if (warnings.Count > 0)
        {
            _logger.LogWarning(() => CircuitBreakerValidationMessage(route) + string.Join(Environment.NewLine, warnings.Select(f => f.Invoke())));
        }

        return true;
    }

    protected virtual string TimeoutValidationMessage(DownstreamRoute route)
        => $"Route '{GetRouteName(route)}' has invalid {nameof(QoSOptions)} for Polly's Timeout strategy. Specifically, ";

    protected virtual bool IsConfigurationValidForTimeout(DownstreamRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(route.QosOptions);
        ArgumentNullException.ThrowIfNull(_globalConfiguration);
        ArgumentNullException.ThrowIfNull(_globalConfiguration.QoSOptions);

        // Gives higher priority to route-level QoS over global ones
        int? timeoutMs = route.QosOptions.TimeoutValue ?? _globalConfiguration.QoSOptions.TimeoutValue; // TODO Move global QoS to QoSOptionsCreator then remove injected IOptions<FileGlobalConfiguration>
        if (!timeoutMs.HasValue || timeoutMs.Value <= 0)
        {
            _logger.LogError(
                () => TimeoutValidationMessage(route) + $"the timeout is disabled because the {nameof(QoSOptions.TimeoutValue)} ({(timeoutMs.HasValue ? timeoutMs.Value.ToString() : "?")}) is either undefined, negative, or zero.", null);
            return false;
        }

        List<Func<string>> warnings = new(), w = warnings;
        if (!timeoutMs.Value.IsValidTimeout())
        {
            string msg() => $"{The(w, msg)} {nameof(TimeoutStrategy.Timeout)} value ({timeoutMs.Value}) is outside the valid range ({TimeoutStrategy.LowTimeout} to {TimeoutStrategy.HighTimeout} milliseconds). Therefore, ensure the value falls within this range; otherwise, the default value ({TimeoutStrategy.DefTimeout}) will be substituted.";
            warnings.Add(msg);
        }

        if (warnings.Count > 0)
        {
            _logger.LogWarning(() => TimeoutValidationMessage(route) + string.Join(Environment.NewLine, warnings.Select(f => f.Invoke())));
        }

        return true;
    }

    private static string The(List<Func<string>> warnings, Func<string> msg)
        => warnings.Count > 1
            ? Environment.NewLine + $"{warnings.IndexOf(msg) + 1}. The"
            : "the";

    protected virtual ResiliencePipelineBuilder<HttpResponseMessage> ConfigureCircuitBreaker(ResiliencePipelineBuilder<HttpResponseMessage> builder, DownstreamRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(route.QosOptions);
        if (!IsConfigurationValidForCircuitBreaker(route))
        {
            return builder;
        }

        var options = route.QosOptions;
        var info = $"Circuit Breaker for the route: {GetRouteName(route)}: ";
        int minimumThroughput = CircuitBreakerStrategy.MinimumThroughput(options.ExceptionsAllowedBeforeBreaking);
        int breakDurationMs = CircuitBreakerStrategy.BreakDuration(options.DurationOfBreak);

        var strategy = new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = 0.8, // options.FailureRatio,
            SamplingDuration = TimeSpan.FromMilliseconds(10_000/*options.SamplingDuration*/),
            MinimumThroughput = minimumThroughput,
            BreakDuration = TimeSpan.FromMilliseconds(breakDurationMs),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(message => ServerErrorCodes.Contains(message.StatusCode))
                .Handle<TimeoutRejectedException>()
                .Handle<TimeoutException>(),
            OnOpened = args =>
            {
                _logger.LogError(info + $"Breaking for {args.BreakDuration.TotalMilliseconds} ms",
                    args.Outcome.Exception);
                return ValueTask.CompletedTask;
            },
            OnClosed = _ =>
            {
                _logger.LogInformation(info + "Closed");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = _ =>
            {
                _logger.LogInformation(info + "Half Opened");
                return ValueTask.CompletedTask;
            },
        };
        return builder.AddCircuitBreaker(strategy);
    }

    /// <summary>Configures the <see href="https://www.pollydocs.org/strategies/timeout.html">Timeout resilience strategy</see>.</summary>
    /// <param name="builder">Pipeline builder instance.</param>
    /// <param name="route">The route the pipeline is applied to.</param>
    /// <returns>The same pipeline builder, as an <see cref="ResiliencePipelineBuilder{HttpResponseMessage}"/> object where TResult is <see cref="HttpResponseMessage"/>.</returns>
    protected virtual ResiliencePipelineBuilder<HttpResponseMessage> ConfigureTimeout(ResiliencePipelineBuilder<HttpResponseMessage> builder, DownstreamRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(route.QosOptions);
        ArgumentNullException.ThrowIfNull(_globalConfiguration);
        ArgumentNullException.ThrowIfNull(_globalConfiguration.QoSOptions);
        if (!IsConfigurationValidForTimeout(route))
        {
            return builder;
        }

        // Gives higher priority to route-level QoS over global ones
        int? timeoutMs = route.QosOptions.TimeoutValue ?? _globalConfiguration.QoSOptions.TimeoutValue ?? TimeoutStrategy.DefaultTimeout; // TODO Move global QoS to QoSOptionsCreator then remove injected IOptions<FileGlobalConfiguration>
        timeoutMs = TimeoutStrategy.Timeout(timeoutMs.Value);

        // Happy path: Set up native qos and apply the strategy
        var strategy = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs.Value),
            OnTimeout = _ =>
            {
                _logger.LogInformation(() => $"Timeout for the route: {GetRouteName(route)}");
                return ValueTask.CompletedTask;
            },
        };
        return builder.AddTimeout(strategy);
    }
}
