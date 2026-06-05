using Ocelot.Configuration;
using Ocelot.Infrastructure.DesignPatterns;
using Ocelot.Logging;

namespace Ocelot.QualityOfService;

/// <summary>
/// A <see cref="DelegatingHandler"/> that implements the <seealso href="https://en.wikipedia.org/wiki/Circuit_breaker_design_pattern">Circuit Breaker</seealso> design pattern
/// and optional per-request timeout for a downstream route.
/// </summary>
/// <remarks>
/// This handler is Ocelot's built-in quality-of-service handler. It wraps every outgoing request and:
/// <list type="bullet">
///   <item>Returns <see cref="System.Net.HttpStatusCode.ServiceUnavailable"/> immediately when the circuit is <see cref="CircuitState.Open"/>.</item>
///   <item>Counts responses whose status code is in <see cref="ServerErrorCodes"/> and exceptions as failures.</item>
///   <item>Opens the circuit after <see cref="QoSOptions.MinimumThroughput"/> consecutive failures.</item>
///   <item>Transitions the circuit to <see cref="CircuitState.HalfOpen"/> after <see cref="QoSOptions.BreakDuration"/> milliseconds.</item>
///   <item>Closes the circuit after a successful probe request in <see cref="CircuitState.HalfOpen"/>.</item>
///   <item>Enforces a per-request timeout when <see cref="QoSOptions.Timeout"/> is configured.</item>
/// </list>
/// </remarks>
public class CircuitBreakerDelegatingHandler : DelegatingHandler
{
    public const int LowBreakDuration = 500;
    public const int DefaultBreakDuration = 5_000;

    public const int LowMinimumThroughput = 2;
    public const int DefaultMinimumThroughput = 100;

    public const double LowFailureRatio = 0.0;
    public const double DefaultFailureRatio = 0.5;

    public const int LowSamplingDuration = 500;
    public const int DefaultSamplingDuration = 10_000;

    public const int LowTimeout = 10;
    public const int DefaultTimeout = 30_000;
    public const int HighTimeout = 86_400_000;

    /// <summary>
    /// The default set of HTTP status codes that are considered server errors and will be counted as circuit-breaker failures.
    /// </summary>
    /// <remarks>
    /// Used by Ocelot's built-in QoS handler to treat downstream 5xx responses as circuit-breaker failures.
    /// Override <see cref="ServerErrorCodes"/> in a subclass to customise which codes are treated as failures.
    /// </remarks>
    public static readonly IReadOnlySet<HttpStatusCode> DefaultServerErrorCodes = new HashSet<HttpStatusCode>
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

    /// <summary>
    /// Gets the set of HTTP status codes treated as failures by this handler.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="DefaultServerErrorCodes"/>. Override in a subclass to customise the failure set.
    /// </remarks>
    protected virtual HashSet<HttpStatusCode> ServerErrorCodes { get; } = (HashSet<HttpStatusCode>)DefaultServerErrorCodes;

    private readonly CircuitBreaker _circuitBreaker;
    private readonly DownstreamRoute _route;
    private readonly QoSOptions _options;
    private readonly IOcelotLogger _logger;
    private readonly int? _timeoutMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerDelegatingHandler"/> class for the specified route.
    /// </summary>
    /// <param name="route">The downstream route whose <see cref="QoSOptions"/> drive circuit-breaker behavior.</param>
    /// <param name="loggerFactory">Factory used to create the handler's logger.</param>
    public CircuitBreakerDelegatingHandler(DownstreamRoute route, IOcelotLoggerFactory loggerFactory)
    {
        _route = route;
        _options = route.QosOptions;
        var breakDuration = GetBreakDuration(_options.BreakDuration ?? DefaultBreakDuration);
        var minimumThroughput = GetMinimumThroughput(_options.MinimumThroughput ?? DefaultMinimumThroughput);

        if (_options.FailureRatio.HasValue && _options.FailureRatio.Value > LowFailureRatio)
        {
            var failureRatio = GetFailureRatio(_options.FailureRatio.Value);
            var samplingDuration = GetSamplingDuration(_options.SamplingDuration ?? DefaultSamplingDuration);
            _circuitBreaker = new CircuitBreaker(failureRatio, minimumThroughput, samplingDuration, breakDuration);
        }
        else
        {
            _circuitBreaker = new CircuitBreaker(minimumThroughput, breakDuration);
        }

        _logger = loggerFactory.CreateLogger<CircuitBreakerDelegatingHandler>();
        _timeoutMs = GetTimeout(_options.Timeout);
    }

    /// <summary>Gets the underlying <see cref="CircuitBreaker"/> instance (exposed for testing purposes).</summary>
    internal CircuitBreaker CircuitBreaker => _circuitBreaker;

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_circuitBreaker.CanExecute())
        {
            _logger.LogWarning(() => $"Circuit breaker is open for '{request.RequestUri}'. Returning {HttpStatusCode.ServiceUnavailable} for route -> {_route.Name()}");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent($"Circuit breaker is open for route -> {_route.Name()}"),
                ReasonPhrase = "Circuit breaker is open",
            };
        }

        if (_timeoutMs.HasValue)
        {
            return await SendWithTimeoutAsync(request, cancellationToken);
        }

        return await SendAndTrackAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithTimeoutAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(_timeoutMs.Value));

        try
        {
            return await SendAndTrackAsync(request, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Our per-request timeout fired (not the outer cancellation token)
            _logger.LogWarning(() => $"Request to '{request.RequestUri}' timed out after {_timeoutMs.Value}ms. Returning {HttpStatusCode.GatewayTimeout} for route -> {_route.Name()}");
            _circuitBreaker.RecordFailure();
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
            {
                Content = new StringContent($"Request timeout for route -> {_route.Name()}"),
                ReasonPhrase = "Request timeout",
            };
        }
    }

    private async Task<HttpResponseMessage> SendAndTrackAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw; // Let timeout/cancellation handling deal with this at the call site
        }
        catch (Exception)
        {
            _circuitBreaker.RecordFailure();
            throw;
        }

        if (ServerErrorCodes.Contains(response.StatusCode))
        {
            _circuitBreaker.RecordFailure();
            _logger.LogInformation(() => $"Circuit breaker recorded failure for '{request.RequestUri}' (status {(int)response.StatusCode}). Failure count is {_circuitBreaker.FailureCount} for route -> {_route.Name()}.");
        }
        else
        {
            _circuitBreaker.RecordSuccess();
            _logger.LogInformation(() => $"Circuit breaker recorded success for '{request.RequestUri}' for route -> {_route.Name()}.");
        }

        return response;
    }

    private static TimeSpan GetBreakDuration(int? milliseconds)
    {
        var ms = milliseconds.HasValue && milliseconds.Value > LowBreakDuration
            ? milliseconds.Value : DefaultBreakDuration;
        return TimeSpan.FromMilliseconds(ms);
    }

    private static int GetMinimumThroughput(int? minimumThroughput)
    {
        var min = minimumThroughput.HasValue && minimumThroughput.Value >= LowMinimumThroughput
            ? minimumThroughput.Value : DefaultMinimumThroughput;
        return min;
    }

    private static double GetFailureRatio(double failureRatio)
        => failureRatio > LowFailureRatio && failureRatio <= 1.0 ? failureRatio : DefaultFailureRatio;

    private static TimeSpan GetSamplingDuration(int? milliseconds)
    {
        var ms = milliseconds.HasValue && milliseconds.Value > LowSamplingDuration
            ? milliseconds.Value : DefaultSamplingDuration;
        return TimeSpan.FromMilliseconds(ms);
    }

    private static int? GetTimeout(int? milliseconds)
    {
        if (!milliseconds.HasValue || milliseconds.Value <= 0)
            return null; // not configured or explicitly disabled
        return milliseconds.Value > LowTimeout && milliseconds.Value < HighTimeout
            ? milliseconds.Value : DefaultTimeout;
    }
}
