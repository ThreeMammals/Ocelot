namespace Ocelot.Infrastructure.DesignPatterns;

/// <summary>
/// Represents the state of the <see cref="CircuitBreaker"/>.
/// </summary>
public enum CircuitState
{
    /// <summary>Normal operation: requests pass through and failures are counted.</summary>
    Closed,

    /// <summary>Circuit is open: requests are blocked and a <see cref="System.Net.HttpStatusCode.ServiceUnavailable"/> is returned immediately.</summary>
    Open,

    /// <summary>Circuit allows one probe request to determine if the downstream is healthy again.</summary>
    HalfOpen,
}

/// <summary>
/// A thread-safe lightweight implementation of the <seealso href="https://en.wikipedia.org/wiki/Circuit_breaker_design_pattern">Circuit Breaker</seealso> design pattern.
/// </summary>
/// <remarks>
/// <para>
/// Docs:
/// <list type="bullet">
///   <item><see href="https://martinfowler.com/bliki/CircuitBreaker.html">Martin Fowler: Circuit Breaker</see></item>
///   <item><see href="https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker">Microsoft Azure: Circuit Breaker pattern</see></item>
///   <item><see href="https://microservices.io/patterns/reliability/circuit-breaker.html">Microservice Architecture: Circuit Breaker</see></item>
/// </list>
/// </para>
/// <para>
/// Inspired by <see href="https://github.com/Netflix/Hystrix/blob/master/hystrix-core/src/main/java/com/netflix/hystrix/HystrixCircuitBreaker.java">HystrixCircuitBreaker</see>.
/// </para>
/// </remarks>
public class CircuitBreaker
{
    private volatile CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private DateTime _openedAt;
    private readonly object _lock = new();

    /// <summary>Gets the minimum number of failures required before the circuit opens.</summary>
    public int MinimumThroughput { get; }

    /// <summary>Gets the duration the circuit remains open before transitioning to <see cref="CircuitState.HalfOpen"/>.</summary>
    public TimeSpan BreakDuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="minimumThroughput">Number of failures before the circuit opens. Use <c>0</c> or negative to disable circuit-opening behavior.</param>
    /// <param name="breakDuration">Duration the circuit stays open before transitioning to <see cref="CircuitState.HalfOpen"/>.</param>
    public CircuitBreaker(int minimumThroughput, TimeSpan breakDuration)
    {
        MinimumThroughput = minimumThroughput;
        BreakDuration = breakDuration;
    }

    /// <summary>Gets the current state of the circuit breaker, transitioning from <see cref="CircuitState.Open"/> to <see cref="CircuitState.HalfOpen"/> when <see cref="BreakDuration"/> has elapsed.</summary>
    public CircuitState State
    {
        get
        {
            if (_state == CircuitState.Open && DateTime.UtcNow - _openedAt >= BreakDuration)
            {
                lock (_lock)
                {
                    if (_state == CircuitState.Open && DateTime.UtcNow - _openedAt >= BreakDuration)
                    {
                        _state = CircuitState.HalfOpen;
                    }
                }
            }

            return _state;
        }
    }

    /// <summary>Returns <see langword="true"/> if a request can proceed (circuit is <see cref="CircuitState.Closed"/> or <see cref="CircuitState.HalfOpen"/>).</summary>
    public bool CanExecute() => State != CircuitState.Open;

    /// <summary>Records a successful request, resetting failure count and closing the circuit.</summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
        }
    }

    /// <summary>Records a failed request. Opens the circuit when <see cref="MinimumThroughput"/> is exceeded or when the circuit is in <see cref="CircuitState.HalfOpen"/>.</summary>
    public void RecordFailure()
    {
        if (MinimumThroughput <= 0)
        {
            return; // Circuit-breaking is disabled when MinimumThroughput is not configured
        }

        lock (_lock)
        {
            _failureCount++;
            if (_state == CircuitState.HalfOpen || (_state == CircuitState.Closed && _failureCount >= MinimumThroughput))
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>Gets the current number of recorded failures (for diagnostic/testing purposes).</summary>
    public int FailureCount => _failureCount;
}
