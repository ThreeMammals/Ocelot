using Ocelot.Infrastructure.DesignPatterns;

namespace Ocelot.UnitTests.Infrastructure.DesignPatterns;

[Trait("Feat", "2384")] // https://github.com/ThreeMammals/Ocelot/issues/2384
[Trait("PR", "2385")] // https://github.com/ThreeMammals/Ocelot/pull/2385
public class CircuitBreakerTests : UnitTest
{
    [Fact]
    public void Constructor_SetsMinimumThroughputAndBreakDuration()
    {
        // Arrange & Act
        var cb = new CircuitBreaker(5, TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(5, cb.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(10), cb.BreakDuration);
    }

    [Fact]
    public void InitialState_IsClosed()
    {
        // Arrange & Act
        var cb = new CircuitBreaker(3, TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.True(cb.CanExecute());
        Assert.Equal(0, cb.FailureCount);
    }

    [Fact]
    public void RecordSuccess_ResetsFailureCountAndKeepsCircuitClosed()
    {
        // Arrange
        var cb = new CircuitBreaker(5, TimeSpan.FromSeconds(10));

        // Act
        cb.RecordFailure();
        cb.RecordFailure();
        cb.RecordSuccess();

        // Assert
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.Equal(0, cb.FailureCount);
        Assert.True(cb.CanExecute());
    }

    [Fact]
    public void RecordFailure_BelowMinimumThroughput_KeepsCircuitClosed()
    {
        // Arrange
        const int minimumThroughput = 3;
        var cb = new CircuitBreaker(minimumThroughput, TimeSpan.FromSeconds(10));

        // Act: record fewer failures than threshold
        cb.RecordFailure();
        cb.RecordFailure();

        // Assert
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.Equal(2, cb.FailureCount);
        Assert.True(cb.CanExecute());
    }

    [Fact]
    public void RecordFailure_AtMinimumThroughput_OpensCircuit()
    {
        // Arrange
        const int minimumThroughput = 3;
        var cb = new CircuitBreaker(minimumThroughput, TimeSpan.FromSeconds(10));

        // Act: record exactly MinimumThroughput failures
        for (int i = 0; i < minimumThroughput; i++)
        {
            cb.RecordFailure();
        }

        // Assert
        Assert.Equal(CircuitState.Open, cb.State);
        Assert.False(cb.CanExecute());
    }

    [Fact]
    public void State_TransitionsToHalfOpen_AfterBreakDurationElapsed()
    {
        // Arrange
        var cb = new CircuitBreaker(1, TimeSpan.FromMilliseconds(50));

        // Act: open the circuit
        cb.RecordFailure();
        Assert.Equal(CircuitState.Open, cb.State);

        // Wait for break duration to elapse
        WaitForState(cb, CircuitState.HalfOpen);

        // Assert: state transitions to HalfOpen when accessed
        Assert.Equal(CircuitState.HalfOpen, cb.State);
        Assert.True(cb.CanExecute());
    }

    [Fact]
    public void RecordSuccess_InHalfOpenState_ClosesCircuit()
    {
        // Arrange
        var cb = new CircuitBreaker(1, TimeSpan.FromMilliseconds(50));

        // Open the circuit
        cb.RecordFailure();
        WaitForState(cb, CircuitState.HalfOpen);

        // Transition to HalfOpen
        Assert.Equal(CircuitState.HalfOpen, cb.State);

        // Act: record success while HalfOpen
        cb.RecordSuccess();

        // Assert: circuit closes
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.Equal(0, cb.FailureCount);
        Assert.True(cb.CanExecute());
    }

    [Fact]
    public void RecordFailure_InHalfOpenState_ReopensCircuit()
    {
        // Arrange
        var cb = new CircuitBreaker(1, TimeSpan.FromMilliseconds(50));

        // Open the circuit then wait for HalfOpen
        cb.RecordFailure();
        Thread.Sleep(100);
        Assert.Equal(CircuitState.HalfOpen, cb.State);

        // Act: record failure while HalfOpen
        cb.RecordFailure();

        // Assert: circuit reopens
        Assert.Equal(CircuitState.Open, cb.State);
        Assert.False(cb.CanExecute());
    }

    [Fact]
    public void RecordFailure_WithZeroMinimumThroughput_NeverOpensCircuit()
    {
        // Arrange: MinimumThroughput = 0 means circuit breaking is disabled
        var cb = new CircuitBreaker(0, TimeSpan.FromSeconds(10));

        // Act: record many failures
        for (int i = 0; i < 100; i++)
        {
            cb.RecordFailure();
        }

        // Assert: circuit stays closed
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.True(cb.CanExecute());
    }

    [Fact]
    public void RecordFailure_WithNegativeMinimumThroughput_NeverOpensCircuit()
    {
        // Arrange: negative MinimumThroughput also disables circuit breaking
        var cb = new CircuitBreaker(-1, TimeSpan.FromSeconds(10));

        // Act: record failures
        for (int i = 0; i < 10; i++)
        {
            cb.RecordFailure();
        }

        // Assert: circuit stays closed
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.True(cb.CanExecute());
    }

    [Fact]
    public void State_RemainsOpen_BeforeBreakDurationElapsed()
    {
        // Arrange
        var cb = new CircuitBreaker(1, TimeSpan.FromSeconds(60));

        // Act: open the circuit
        cb.RecordFailure();

        // Assert: state remains Open (break duration has not elapsed)
        Assert.Equal(CircuitState.Open, cb.State);
        Assert.False(cb.CanExecute());
    }

    [Fact]
    public void FullLifecycle_OpenHalfOpenClosedCycle_WorksCorrectly()
    {
        // Arrange
        const int minimumThroughput = 2;
        var cb = new CircuitBreaker(minimumThroughput, TimeSpan.FromMilliseconds(50));

        // Step 1: Closed → failures counted
        Assert.Equal(CircuitState.Closed, cb.State);
        cb.RecordFailure();
        Assert.Equal(CircuitState.Closed, cb.State);

        // Step 2: Closed → Open (at MinimumThroughput)
        cb.RecordFailure();
        Assert.Equal(CircuitState.Open, cb.State);
        Assert.False(cb.CanExecute());

        // Step 3: Open → HalfOpen (after BreakDuration)
        Thread.Sleep(100);
        Assert.Equal(CircuitState.HalfOpen, cb.State);
        Assert.True(cb.CanExecute());

        // Step 4: HalfOpen → Closed (success)
        cb.RecordSuccess();
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.Equal(0, cb.FailureCount);
        Assert.True(cb.CanExecute());
    }

    private static void WaitForState(CircuitBreaker cb, CircuitState expectedState, int timeoutMs = 1000)
    {
        var reachedState = SpinWait.SpinUntil(() => cb.State == expectedState, timeoutMs);
        Assert.True(reachedState, $"Expected circuit to transition to {expectedState} within {timeoutMs}ms, but was {cb.State}.");
    }

    // ───────────────────────────────────────────────────────────────────────────────
    //  Ratio mode
    // ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_RatioMode_SetsAllProperties()
    {
        // Arrange & Act
        var cb = new CircuitBreaker(0.5, 10, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(0.5, cb.FailureRatio);
        Assert.Equal(10, cb.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(10), cb.SamplingDuration);
        Assert.Equal(TimeSpan.FromSeconds(30), cb.BreakDuration);
        Assert.Equal(CircuitState.Closed, cb.State);
    }

    [Fact]
    public void RatioMode_InitialState_IsClosed()
    {
        var cb = new CircuitBreaker(0.5, 5, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.True(cb.CanExecute());
        Assert.Equal(0, cb.FailureCount);
        Assert.Equal(0, cb.TotalCount);
    }

    [Fact]
    public void RatioMode_BelowMinimumThroughput_KeepsCircuitClosed()
    {
        // Arrange: need at least 5 requests before evaluating
        var cb = new CircuitBreaker(0.5, 5, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        // Act: 4 failures — below MinimumThroughput of 5
        cb.RecordFailure();
        cb.RecordFailure();
        cb.RecordFailure();
        cb.RecordFailure();

        // Assert: circuit stays closed — minimum throughput not reached
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.True(cb.CanExecute());
        Assert.Equal(4, cb.FailureCount);
    }

    [Fact]
    public void RatioMode_AboveMinimumThroughput_RatioBelowThreshold_KeepsCircuitClosed()
    {
        // Arrange: FailureRatio = 0.5 (50%), MinimumThroughput = 4
        var cb = new CircuitBreaker(0.5, 4, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        // Act: 1 failure + 3 successes = 25% failure ratio (below 50%)
        cb.RecordFailure();
        cb.RecordSuccess();
        cb.RecordSuccess();
        cb.RecordSuccess();

        // Assert: ratio 1/4 = 0.25 < 0.5 → circuit stays closed
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.True(cb.CanExecute());
    }

    [Fact]
    public void RatioMode_AtMinimumThroughput_RatioAtThreshold_OpensCircuit()
    {
        // Arrange: FailureRatio = 0.5 (50%), MinimumThroughput = 4
        var cb = new CircuitBreaker(0.5, 4, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        // Act: 2 failures + 2 successes = 50% failure ratio
        cb.RecordSuccess();
        cb.RecordSuccess();
        cb.RecordFailure();
        cb.RecordFailure();

        // Assert: ratio 2/4 = 0.5 >= 0.5 → circuit opens
        Assert.Equal(CircuitState.Open, cb.State);
        Assert.False(cb.CanExecute());
    }

    [Fact]
    public void RatioMode_AboveMinimumThroughput_RatioAboveThreshold_OpensCircuit()
    {
        // Arrange: FailureRatio = 0.5 (50%), MinimumThroughput = 2
        var cb = new CircuitBreaker(0.5, 2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        // Act: 2 failures = 100% failure ratio (above 50%)
        cb.RecordFailure();
        cb.RecordFailure();

        // Assert: ratio 2/2 = 1.0 >= 0.5 → circuit opens
        Assert.Equal(CircuitState.Open, cb.State);
        Assert.False(cb.CanExecute());
    }

    [Fact]
    public void RatioMode_RecordFailure_InHalfOpen_ReopensCircuit()
    {
        // Arrange: open circuit quickly with short break duration
        var cb = new CircuitBreaker(0.5, 2, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(50));
        cb.RecordFailure();
        cb.RecordFailure();
        Assert.Equal(CircuitState.Open, cb.State);

        // Wait for HalfOpen
        WaitForState(cb, CircuitState.HalfOpen);
        Assert.Equal(CircuitState.HalfOpen, cb.State);

        // Act: failure during probe
        cb.RecordFailure();

        // Assert: circuit reopens
        Assert.Equal(CircuitState.Open, cb.State);
        Assert.False(cb.CanExecute());
    }

    [Fact]
    public void RatioMode_RecordSuccess_InHalfOpen_ClosesCircuitAndResetsWindow()
    {
        // Arrange: open circuit, then transition to HalfOpen
        var cb = new CircuitBreaker(0.5, 2, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(50));
        cb.RecordFailure();
        cb.RecordFailure();
        WaitForState(cb, CircuitState.HalfOpen);

        // Act: successful probe
        cb.RecordSuccess();

        // Assert: circuit closes and window is reset
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.True(cb.CanExecute());
        Assert.Equal(0, cb.FailureCount);
        Assert.Equal(0, cb.TotalCount);
    }

    [Fact]
    public void RatioMode_OldWindowEntriesExpire_DoNotContributeToRatio()
    {
        // Arrange: short sampling window of 100ms
        var cb = new CircuitBreaker(0.5, 2, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(60));

        // Record two failures (enough to open if they stay in window)
        cb.RecordFailure();
        cb.RecordFailure();
        Assert.Equal(CircuitState.Open, cb.State);

        // Recover: wait for break duration (60s is too long — use a short break duration circuit for this test).
        // Instead, verify that entries in the window expire after SamplingDuration.
        // (This test focuses on window purging for a fresh Closed circuit.)
        var cb2 = new CircuitBreaker(0.5, 3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(60));

        // Record 2 failures — below MinimumThroughput of 3
        cb2.RecordFailure();
        cb2.RecordFailure();
        Assert.Equal(CircuitState.Closed, cb2.State);

        // Wait for sampling window to expire
        Thread.Sleep(150);

        // Now record 1 failure — old entries are purged, window now has only 1 failure out of 1 total.
        // That IS >= 0.5, but total (1) < MinimumThroughput (3), so circuit stays closed.
        cb2.RecordFailure();
        Assert.Equal(CircuitState.Closed, cb2.State);
        Assert.Equal(1, cb2.FailureCount);
        Assert.Equal(1, cb2.TotalCount);
    }

    [Fact]
    public void RatioMode_FullLifecycle_WorksCorrectly()
    {
        // Arrange: ratio=0.5, throughput=4, short break duration
        var cb = new CircuitBreaker(0.5, 4, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(50));

        // Step 1: Build up requests — below MinimumThroughput, stays Closed
        cb.RecordSuccess();
        cb.RecordFailure();
        Assert.Equal(CircuitState.Closed, cb.State);

        // Step 2: Reach MinimumThroughput with ratio >= 0.5 → Open
        cb.RecordFailure();
        cb.RecordFailure(); // now 3/4 failures = 75% → opens
        Assert.Equal(CircuitState.Open, cb.State);
        Assert.False(cb.CanExecute());

        // Step 3: Wait for BreakDuration → HalfOpen
        WaitForState(cb, CircuitState.HalfOpen);
        Assert.True(cb.CanExecute());

        // Step 4: Successful probe → Closed
        cb.RecordSuccess();
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.Equal(0, cb.FailureCount);
    }
}
