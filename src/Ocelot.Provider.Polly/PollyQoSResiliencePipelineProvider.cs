using System.Net;

using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;

using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public class PollyQoSResiliencePipelineProvider : IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
{
    private readonly ResiliencePipelineRegistry<OcelotResiliencePipelineKey> _resiliencePipelineRegistry;
    private readonly IOcelotLogger _logger;

    private static readonly HashSet<HttpStatusCode> ServerErrorCodes = new()
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

    public PollyQoSResiliencePipelineProvider(IOcelotLoggerFactory loggerFactory, 
        ResiliencePipelineRegistry<OcelotResiliencePipelineKey> resiliencePipelineRegistry)
    {
        _resiliencePipelineRegistry = resiliencePipelineRegistry;
        _logger = loggerFactory.CreateLogger<PollyQoSResiliencePipelineProvider>();
    }

    public ResiliencePipeline<HttpResponseMessage> GetResiliencePipeline(DownstreamRoute route)
    {
        var options = route.QosOptions;
        // do the check if we need pipeline at all before calling GetOrAddPipeline
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
        // add TimeoutStrategy if TimeoutValue is not int.MaxValue and greater than 0
        if (route.QosOptions.TimeoutValue != int.MaxValue && route.QosOptions.TimeoutValue > 0)
        {
            builder.AddTimeout(TimeSpan.FromMilliseconds(route.QosOptions.TimeoutValue));
        }

        // add CircuitBreakerStrategy only if ExceptionsAllowedBeforeBreaking is greater than 0
        if (route.QosOptions.ExceptionsAllowedBeforeBreaking <= 0)
        {
            return; // shortcut > no qos (no timeout, no ExceptionsAllowedBeforeBreaking)
        }

        var info = $"Circuit Breaker for Route: {GetRouteName(route)}:";

        var circuitBreakerStrategyOptions = new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = 0.8,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = route.QosOptions.ExceptionsAllowedBeforeBreaking, 
            BreakDuration = TimeSpan.FromMilliseconds(route.QosOptions.DurationOfBreak),
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
        };

        builder.AddCircuitBreaker(circuitBreakerStrategyOptions);
    }

    private static string GetRouteName(DownstreamRoute route)
        => string.IsNullOrWhiteSpace(route.ServiceName)
            ? route.UpstreamPathTemplate?.Template ?? route.DownstreamPathTemplate?.Value ?? string.Empty
            : route.ServiceName;
}
