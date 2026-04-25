namespace Ocelot.Infrastructure.DesignPatterns;

/// <summary>
/// Represents the state of the <see cref="CircuitBreaker"/>.
/// </summary>
public enum CircuitState
{
    /// <summary>Normal operation: requests pass through and failures are counted.</summary>
    Closed,

    /// <summary>Circuit is open: requests are blocked and a <see cref="HttpStatusCode.ServiceUnavailable"/> is returned immediately.</summary>
    Open,

    /// <summary>Circuit allows one probe request to determine if the downstream is healthy again.</summary>
    HalfOpen,
}

/// <summary>
/// A thread-safe lightweight implementation of the <seealso href="https://en.wikipedia.org/wiki/Circuit_breaker_design_pattern">Circuit Breaker</seealso> design pattern.
/// </summary>
/// <remarks>
/// <para>
/// Supports two operating modes:
/// <list type="bullet">
///   <item><b>Count mode</b> (default): opens the circuit after <see cref="MinimumThroughput"/> consecutive failures.</item>
///   <item><b>Ratio mode</b>: opens the circuit when the failure ratio within a rolling <see cref="SamplingDuration"/> window
///   reaches or exceeds <see cref="FailureRatio"/>, provided at least <see cref="MinimumThroughput"/> requests have been made
///   in that window. Activated by passing <see cref="FailureRatio"/> and <see cref="SamplingDuration"/> to the constructor.</item>
/// </list>
/// </para>
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
    private DateTime _openedAt;
    private readonly object _lock = new();

    // ── HalfOpen one-probe gate ─────────────────────────────────────────
    // Ensures exactly one probe request is allowed when the circuit is HalfOpen.
    // Reset to false whenever the circuit transitions into HalfOpen so that the
    // first CanExecute() call can set it to true and proceed; all concurrent
    // callers that also see HalfOpen are blocked until the probe completes.
    private bool _halfOpenProbeInFlight;

    // ── Count-mode state ────────────────────────────────────────────────
    private int _failureCount;

    // ── Ratio-mode state ────────────────────────────────────────────────
    private readonly Queue<(DateTime timestamp, bool isFailure)> _window;
    private int _windowFailureCount;
    private int _windowTotalCount;

    /// <summary>Gets the minimum number of requests (ratio mode) or failures (count mode) required before the circuit can open.</summary>
    public int MinimumThroughput { get; }

    /// <summary>Gets the duration the circuit remains open before transitioning to <see cref="CircuitState.HalfOpen"/>.</summary>
    public TimeSpan BreakDuration { get; }

    /// <summary>Gets the failure-to-total ratio threshold at which the circuit opens (ratio mode only).</summary>
    /// <value><see langword="null"/> when operating in count mode.</value>
    public double? FailureRatio { get; }

    /// <summary>Gets the duration of the rolling window used to evaluate <see cref="FailureRatio"/> (ratio mode only).</summary>
    /// <value><see langword="null"/> when operating in count mode.</value>
    public TimeSpan? SamplingDuration { get; }
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class in <b>count mode</b>.
    /// </summary>
    /// <param name="minimumThroughput">Number of failures before the circuit opens. Use <c>0</c> or negative to disable circuit-opening behavior.</param>
    /// <param name="breakDuration">Duration the circuit stays open before transitioning to <see cref="CircuitState.HalfOpen"/>.</param>
    public CircuitBreaker(int minimumThroughput, TimeSpan breakDuration)
    {
        MinimumThroughput = minimumThroughput;
        BreakDuration = breakDuration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class in <b>ratio mode</b>.
    /// </summary>
    /// <param name="failureRatio">Failure-to-total ratio (in range (0.0, 1.0]) at which the circuit opens.</param>
    /// <param name="minimumThroughput">Minimum number of requests within <paramref name="samplingDuration"/> before the ratio is evaluated.</param>
    /// <param name="samplingDuration">Duration of the rolling window over which <paramref name="failureRatio"/> is assessed.</param>
    /// <param name="breakDuration">Duration the circuit stays open before transitioning to <see cref="CircuitState.HalfOpen"/>.</param>
    public CircuitBreaker(double failureRatio, int minimumThroughput, TimeSpan samplingDuration, TimeSpan breakDuration)
    {
        FailureRatio = failureRatio;
        MinimumThroughput = minimumThroughput;
        SamplingDuration = samplingDuration;
        BreakDuration = breakDuration;
        _window = new Queue<(DateTime, bool)>();
    }

    /// <summary>Gets the current state of the circuit breaker, transitioning from <see cref="CircuitState.Open"/> to <see cref="CircuitState.HalfOpen"/> when <see cref="BreakDuration"/> has elapsed.</summary>
    /// <remarks>
    /// When transitioning to <see cref="CircuitState.HalfOpen"/> the one-probe gate is reset so that
    /// the next <see cref="CanExecute"/> call can claim the single probe slot.
    /// </remarks>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open && DateTime.UtcNow - _openedAt >= BreakDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _halfOpenProbeInFlight = false; // Reset probe gate for the new HalfOpen window
                }

                return _state;
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if a request can proceed, and <see langword="false"/> if it should be rejected.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><b>Closed</b>: always returns <see langword="true"/>.</item>
    ///   <item><b>Open</b>: returns <see langword="false"/>. Once <see cref="BreakDuration"/> elapses the circuit
    ///   internally transitions to <see cref="CircuitState.HalfOpen"/> and the next rule applies.</item>
    ///   <item><b>HalfOpen</b>: returns <see langword="true"/> for exactly <em>one</em> concurrent probe request
    ///   (the first caller atomically claims the probe slot); all subsequent concurrent callers receive
    ///   <see langword="false"/> until the probe either succeeds (circuit closes) or fails (circuit reopens).</item>
    /// </list>
    /// This single-probe semantic matches the behaviour of Netflix Hystrix
    /// (<c>attemptExecution()</c> with <c>compareAndSet(OPEN, HALF_OPEN)</c>) and Polly's circuit-breaker
    /// strategy, and prevents a "thundering-herd" of probes from flooding an already-struggling downstream.
    /// </remarks>
    public bool CanExecute()
    {
        lock (_lock)
        {
            // Lazy Open → HalfOpen transition (mirrors the State getter).
            if (_state == CircuitState.Open && DateTime.UtcNow - _openedAt >= BreakDuration)
            {
                _state = CircuitState.HalfOpen;
                _halfOpenProbeInFlight = false;
            }

            if (_state == CircuitState.Closed)
                return true;

            if (_state == CircuitState.HalfOpen && !_halfOpenProbeInFlight)
            {
                _halfOpenProbeInFlight = true; // Claim the single probe slot
                return true;
            }

            return false; // Open, or HalfOpen with a probe already in flight
        }
    }

    /// <summary>Records a successful request.</summary>
    /// <remarks>
    /// In count mode: resets the failure count and closes the circuit.
    /// In ratio mode: adds the request to the rolling window. If the circuit is <see cref="CircuitState.HalfOpen"/>, closes it and resets the window.
    /// </remarks>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_window != null)
            {
                PurgeOldEntries();
                _window.Enqueue((DateTime.UtcNow, false));
                _windowTotalCount++;

                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Closed;
                    _halfOpenProbeInFlight = false;
                    // Clear the window after a successful probe so ratio resets cleanly.
                    _window.Clear();
                    _windowFailureCount = 0;
                    _windowTotalCount = 0;
                }
            }
            else
            {
                _failureCount = 0;
                _halfOpenProbeInFlight = false;
                _state = CircuitState.Closed;
            }
        }
    }

    /// <summary>Records a failed request.</summary>
    /// <remarks>
    /// In count mode: opens the circuit when <see cref="MinimumThroughput"/> is reached or when the circuit is in <see cref="CircuitState.HalfOpen"/>.
    /// In ratio mode: opens the circuit when total requests in the rolling window reaches <see cref="MinimumThroughput"/> and the failure ratio reaches <see cref="FailureRatio"/>, or immediately when in <see cref="CircuitState.HalfOpen"/>.
    /// </remarks>
    public void RecordFailure()
    {
        if (_window != null)
        {
            RecordFailureRatioMode();
        }
        else
        {
            RecordFailureCountMode();
        }
    }

    private void RecordFailureCountMode()
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
                _openedAt = DateTime.UtcNow;
                _state = CircuitState.Open;
            }
        }
    }

    private void RecordFailureRatioMode()
    {
        lock (_lock)
        {
            PurgeOldEntries();
            _window.Enqueue((DateTime.UtcNow, true));
            _windowFailureCount++;
            _windowTotalCount++;

            if (_state == CircuitState.HalfOpen)
            {
                // Any failure during the probe request reopens the circuit immediately.
                _openedAt = DateTime.UtcNow;
                _state = CircuitState.Open;
                return;
            }

            if (_state == CircuitState.Closed
                && _windowTotalCount >= MinimumThroughput
                && (double)_windowFailureCount / _windowTotalCount >= FailureRatio!.Value)
            {
                _openedAt = DateTime.UtcNow;
                _state = CircuitState.Open;
            }
        }
    }

    /// <summary>Removes window entries older than <see cref="SamplingDuration"/>. Must be called within <c>_lock</c>.</summary>
    private void PurgeOldEntries()
    {
        var cutoff = DateTime.UtcNow - SamplingDuration.Value;
        while (_window.Count > 0 && _window.Peek().timestamp < cutoff)
        {
            var entry = _window.Dequeue();
            _windowTotalCount--;
            if (entry.isFailure)
            {
                _windowFailureCount--;
            }
        }
    }

    /// <summary>Gets the current number of recorded failures within the active window (for diagnostic/testing purposes).</summary>
    public int FailureCount => _window != null ? _windowFailureCount : _failureCount;

    /// <summary>Gets the total number of requests recorded in the rolling window (ratio mode only, for diagnostic/testing purposes).</summary>
    public int TotalCount => _windowTotalCount;
}
