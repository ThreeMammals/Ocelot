using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

/// <summary>
/// Default provider for Polly V8 pipelines.
/// </summary>
public class PollyQoSResiliencePipelineProvider : PollyQoSProviderBase, IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
{
    private readonly ResiliencePipelineRegistry<OcelotResiliencePipelineKey> _resiliencePipelineRegistry;
    private readonly IOcelotLogger _logger;

    public PollyQoSResiliencePipelineProvider(IOcelotLoggerFactory loggerFactory,
        ResiliencePipelineRegistry<OcelotResiliencePipelineKey> resiliencePipelineRegistry)
    {
        _resiliencePipelineRegistry = resiliencePipelineRegistry;
        _logger = loggerFactory.CreateLogger<PollyQoSResiliencePipelineProvider>();
    }

    /// <summary>
    /// Gets Polly V8 resilience pipeline (applies QoS feature) for the route.
    /// </summary>
    /// <param name="route">The downstream route to apply the pipeline for.</param>
    /// <returns>A <see cref="ResiliencePipeline{T}"/> object where T is <see cref="HttpResponseMessage"/>.</returns>
    public ResiliencePipeline<HttpResponseMessage> GetResiliencePipeline(DownstreamRoute route)
    {
        var options = route.QosOptions;

        // Check if we need pipeline at all before calling GetOrAddPipeline
        if (options is null ||
            (options.ExceptionsAllowedBeforeBreaking == 0 && options.TimeoutValue is int.MaxValue))
        {
            return null; // shortcut > no qos
        }

        var currentRouteName = GetRouteName(route);
        return _resiliencePipelineRegistry.GetOrAddPipeline<HttpResponseMessage>(
            key: new OcelotResiliencePipelineKey(currentRouteName),
            configure: (builder) => PollyResiliencePipelineWrapperFactory(builder, route));
    }

    private void PollyResiliencePipelineWrapperFactory(ResiliencePipelineBuilder<HttpResponseMessage> builder, DownstreamRoute route)
    {
        var options = route.QosOptions;

     

        // Add CircuitBreakerStrategy only if ExceptionsAllowedBeforeBreaking is greater than 2
        if (options.ExceptionsAllowedBeforeBreaking >= 2)
        {
            // shortcut > no qos (no timeout, no ExceptionsAllowedBeforeBreaking)
            var info = $"Circuit Breaker for Route: {GetRouteName(route)}: ";

            var circuitBreakerStrategyOptions = new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.8,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = options.ExceptionsAllowedBeforeBreaking,
                BreakDuration = TimeSpan.FromMilliseconds(options.DurationOfBreak),
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
                }
            };

            builder.AddCircuitBreaker(circuitBreakerStrategyOptions);
        }

        // Add TimeoutStrategy if TimeoutValue is not int.MaxValue and greater than 0
        if (options.TimeoutValue != int.MaxValue && options.TimeoutValue > 0)
        {
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMilliseconds(options.TimeoutValue),
                OnTimeout = _ =>
                {
                    _logger.LogInformation($"Timeout for Route: {GetRouteName(route)}");
                    return ValueTask.CompletedTask;
                }
            });
        }
    }
}
