using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;
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
        _logger = loggerFactory.CreateLogger<PollyQoSResiliencePipelineProvider>();
        _registry = registry;
        _globalConfiguration = global.Value;
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
        var options = route?.QosOptions;
        if (options is null || !options.UseQos)
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

    protected virtual ResiliencePipelineBuilder<HttpResponseMessage> ConfigureCircuitBreaker(ResiliencePipelineBuilder<HttpResponseMessage> builder, DownstreamRoute route)
    {
        // Add CircuitBreaker strategy only if ExceptionsAllowedBeforeBreaking is greater/equal than/to 2
        var options = route.QosOptions;
        if (options.ExceptionsAllowedBeforeBreaking < 2)
        {
            return builder;
        }

        var info = $"Circuit Breaker for the route: {GetRouteName(route)}: ";
        int minimumThroughput = CircuitBreakerStrategy.MinimumThroughput(options.ExceptionsAllowedBeforeBreaking);
        int breakDurationMs = CircuitBreakerStrategy.BreakDuration(options.DurationOfBreak);

        var strategy = new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = 0.8,
            SamplingDuration = TimeSpan.FromSeconds(10),
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
        // Gives higher priority to route-level options over global ones
        int? timeoutMs = route?.QosOptions?.TimeoutValue ?? _globalConfiguration?.QoSOptions?.TimeoutValue;

        // Short cut: don't apply the strategy if no QoS options
        if (!timeoutMs.HasValue || timeoutMs.Value <= 0)
        {
            return builder;
        }

        // Polly docs -> https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout
        timeoutMs = TimeoutStrategy.ApplyConstraint(timeoutMs.Value);

        // Happy path: Set up native options and apply the strategy
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
