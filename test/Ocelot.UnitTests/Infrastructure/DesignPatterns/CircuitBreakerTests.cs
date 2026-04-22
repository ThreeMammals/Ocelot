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
}
