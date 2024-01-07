using System.Net;

using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;

using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public class PollyQoSResiliencePipelineProvider : IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
{
    private readonly Dictionary<string, ResiliencePipeline<HttpResponseMessage>> _resiliencePipelineWrappers = new();

    private readonly object _lockObject = new();
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

    public PollyQoSResiliencePipelineProvider(IOcelotLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PollyQoSResiliencePipelineProvider>();
    }

    private static string GetRouteName(DownstreamRoute route)
        => string.IsNullOrWhiteSpace(route.ServiceName)
            ? route.UpstreamPathTemplate?.Template ?? route.DownstreamPathTemplate?.Value ?? string.Empty
            : route.ServiceName;

    public ResiliencePipeline<HttpResponseMessage> GetResiliencePipeline(DownstreamRoute route)
    {
        lock (_lockObject)
        {
            var currentRouteName = GetRouteName(route);
            if (!_resiliencePipelineWrappers.ContainsKey(currentRouteName))
            {
                _resiliencePipelineWrappers.Add(currentRouteName, PollyResiliencePipelineWrapperFactory(route));
            }

            return _resiliencePipelineWrappers[currentRouteName];
        }
    }

    private ResiliencePipeline<HttpResponseMessage> PollyResiliencePipelineWrapperFactory(DownstreamRoute route)
    {
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>();

        if (route.QosOptions.TimeoutValue != int.MaxValue && route.QosOptions.TimeoutValue > 0)
        {
            pipeline.AddTimeout(TimeSpan.FromMilliseconds(route.QosOptions.TimeoutValue));
        }
        else if (route.QosOptions.ExceptionsAllowedBeforeBreaking == 0)
        {
            return null; // shortcut > no qos
        }

        if (route.QosOptions.ExceptionsAllowedBeforeBreaking > 0)
        {
            var info = $"Circuit Breaker for Route: {GetRouteName(route)}:";
       
            var circuitBreakerStrategyOptions = new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.8,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 2, //route.QosOptions.ExceptionsAllowedBeforeBreaking,
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

            pipeline = pipeline.AddCircuitBreaker(circuitBreakerStrategyOptions);
        }

        return pipeline.Build();
    }
}
