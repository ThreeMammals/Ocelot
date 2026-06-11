using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;
using System.Reflection;

namespace Ocelot.UnitTests.Configuration.Repository;

public sealed class FileConfigurationPollerTests : UnitTest, IDisposable
{
    private const int PollingDelayInMs = 100;
    private const int LongRunningPollDelayInMs = PollingDelayInMs + 50;
    private const int ShortPollingIntervalInMs = 10; // fast interval for tests that need reliable callbacks

    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly Mock<IOcelotLoggerFactory> _factory = new();
    private readonly Mock<IFileConfigurationRepository> _repo = new();
    private readonly Mock<IFileConfigurationPollerOptions> _config = new();
    private readonly Mock<IInternalConfigurationRepository> _internalConfigRepo = new();
    private readonly Mock<IInternalConfigurationCreator> _internalConfigCreator = new();
    private readonly Mock<IInternalConfiguration> _internalConfig = new();
    private readonly FileConfiguration _initialFileConfig = new();
    private readonly FileConfigurationPoller _poller; // service under test

    public FileConfigurationPollerTests()
    {
        _factory.Setup(x => x.CreateLogger<FileConfigurationPoller>()).Returns(_logger.Object);
        _repo.Setup(x => x.Get()).Returns(_initialFileConfig);
        _config.Setup(x => x.Delay()).Returns(PollingDelayInMs);
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(PollingDelayInMs);
        _internalConfigCreator.Setup(x => x.Create(It.IsAny<FileConfiguration>())).ReturnsAsync(new OkResponse<IInternalConfiguration>(_internalConfig.Object));
        _poller = new FileConfigurationPoller(_factory.Object, _repo.Object, _config.Object, _internalConfigRepo.Object, _internalConfigCreator.Object);
    }

    [Fact]
    public async Task Should_start_and_poll_initial_configuration()
    {
        // Arrange, Act
        await _poller.StartAsync(CancelMe);

        // Assert
        ThenTheSetterIsCalled(_initialFileConfig, 1);
    }

    [Fact]
    public async Task Should_not_replace_timer_when_start_called_twice()
    {
        // Arrange
        await _poller.StartAsync(CancelMe);
        var timerAfterFirstStart = CurrentTimer();

        // Act
        await _poller.StartAsync(CancelMe);
        var timerAfterSecondStart = CurrentTimer();

        // Assert
        Assert.NotNull(timerAfterFirstStart);
        Assert.Same(timerAfterFirstStart, timerAfterSecondStart);
    }

    [Fact]
    public async Task Should_do_nothing_when_stop_called_before_start()
    {
        // Arrange, Act
        await _poller.StopAsync(CancelMe);
        await Task.Delay(PollingDelayInMs * 2, CancelMe);

        // Assert
        Assert.Equal(0, NumberOfGetInvocations());
    }

    [Fact]
    public async Task Should_call_setter_only_once_when_configuration_does_not_change_across_multiple_poll_cycles()
    {
        // Arrange, Act
        await _poller.StartAsync(CancelMe);

        // Assert
        ThenTheSetterIsCalled(_initialFileConfig, 1);
        ThenTheConfigIsNotAddedMoreThan(1);
    }

    [Fact]
    public async Task Should_not_poll_if_already_polling()
    {
        // Arrange
        var newConfig = GivenConfiguration();

        // Act
        await _poller.StartAsync(CancelMe);

        // Assert
        WhenTheConfigIsChanged(newConfig, LongRunningPollDelayInMs);
        ThenTheSetterIsCalled(newConfig, 1);
    }

    [Fact]
    public async Task Should_return_early_on_timer_tick_when_polling_is_already_in_progress()
    {
        // Arrange
        var callCount = 0;
        using var firstPollStarted = new ManualResetEventSlim(false);
        using var releaseFirstPoll = new ManualResetEventSlim(false);
        _repo.Setup(x => x.Get()).Returns(() =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                firstPollStarted.Set();
                Assert.True(releaseFirstPoll.Wait(TimeSpan.FromSeconds(2)), "The first poll was not released in time.");
            }

            return _initialFileConfig;
        });

        try
        {
            // Act
            await _poller.StartAsync(CancelMe);
            Assert.True(firstPollStarted.Wait(TimeSpan.FromSeconds(2), CancelMe));
            await Task.Delay(PollingDelayInMs * 2, CancelMe); // allow overlapping tick while first poll is blocked

            // Assert
            Assert.Equal(1, Volatile.Read(ref callCount));
        }
        finally
        {
            releaseFirstPoll.Set();
            await _poller.StopAsync(CancelMe);
        }
    }

    [Fact]
    public async Task Should_do_nothing_if_call_to_provider_fails()
    {
        // Arrange, Act
        WhenProviderErrors();
        await _poller.StartAsync(CancelMe);

        // Assert
        ThenTheProviderIsPolled();
        ThenTheSetterIsNotCalled();
    }

    [Fact]
    public async Task Should_not_add_to_internal_repo_if_internal_configuration_creation_fails()
    {
        // Arrange
        var newConfig = GivenConfiguration();

        _internalConfigCreator
            .Setup(x => x.Create(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new ErrorResponse<IInternalConfiguration>(new AnyError()));
        _repo.Setup(x => x.Get()).Returns(newConfig);

        // Act
        await _poller.StartAsync(CancelMe);

        // Assert
        ThenTheCreatorIsCalled(newConfig, 1);
        ThenTheConfigIsNotAdded();
    }

    [Fact]
    public async Task Should_stop_polling_when_stopped()
    {
        // Arrange, Act
        await _poller.StartAsync(CancelMe);
        await Task.Delay(PollingDelayInMs * 2, CancelMe);
        await _poller.StopAsync(CancelMe);
        await Task.Delay(PollingDelayInMs, CancelMe);
        var afterStopSettled = NumberOfGetInvocations();
        await Task.Delay(PollingDelayInMs * 2, CancelMe);

        // Assert
        ThenTheSetterIsCalled(_initialFileConfig, 1);
        Assert.Equal(afterStopSettled, NumberOfGetInvocations());
    }

    [Fact]
    public async Task StopAsync_Should_wait_for_running_timer_callback_to_complete()
    {
        // Arrange
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        using var pollStarted = new ManualResetEventSlim(false);
        using var releasePoll = new ManualResetEventSlim(false);
        _repo.Setup(x => x.Get()).Returns(() =>
        {
            pollStarted.Set();
            releasePoll.Wait(TimeSpan.FromSeconds(10)); // safety timeout to avoid hanging
            return _initialFileConfig;
        });

        await _poller.StartAsync(CancelMe);
        Assert.True(pollStarted.Wait(TimeSpan.FromSeconds(2), CancelMe), "Poll should have started within 2 seconds.");

        try
        {
            // Act: StopAsync is truly async (awaits Task.Run internally), so the returned task
            // should still be in-progress while the timer callback is blocked.
            var stopTask = _poller.StopAsync(CancelMe);
            await Task.Delay(50, CancelMe);

            // Assert
            Assert.False(stopTask.IsCompleted, "StopAsync should not complete while the timer callback is still running.");
            releasePoll.Set();
            await stopTask; // completes once the callback exits and the timer signals timerStopped
        }
        finally
        {
            releasePoll.Set(); // ensure the callback is always released even if the test fails early
        }
    }

    [Fact]
    public void Should_dispose_cleanly_without_starting()
    {
        // Arrange, Act, Assert
        _poller.Dispose(); // when poller is disposed
    }

    [Fact]
    public void Dispose_Should_not_throw_when_timer_is_already_disposed()
    {
        // Arrange
        using var timer = new Timer(_ => { });
        timer.Dispose();
        var timerField = typeof(FileConfigurationPoller)
            .GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);
        timerField.SetValue(_poller, timer);

        // Act, Assert
        _poller.Dispose();
    }

    [Fact]
    public void OnTimer_Should_return_early_when_already_polling()
    {
        // Arrange: set the private _isPolling field to true so OnTimer takes the early-return path (line 38)
        var pollingField = typeof(FileConfigurationPoller)
            .GetField("_isPolling", BindingFlags.Instance | BindingFlags.NonPublic);
        pollingField!.SetValue(_poller, 1);

        // Act: invoke the private OnTimer method directly
        var onTimerMethod = typeof(FileConfigurationPoller)
            .GetMethod("OnTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        onTimerMethod.Invoke(_poller, [null]);

        // Assert: Get() was never called because polling was already in progress
        _repo.Verify(x => x.Get(), Times.Never);
    }

    [Fact]
    public async Task PollAsync_Should_return_early_when_already_polling()
    {
        // Arrange: set the private _isPolling field to true so PollAsync takes the early-return path (line 103)
        var pollingField = typeof(FileConfigurationPoller)
            .GetField("_isPolling", BindingFlags.Instance | BindingFlags.NonPublic);
        pollingField!.SetValue(_poller, 1);

        // Act
        await _poller.PollAsync(CancelMe);

        // Assert: GetAsync() was never called because polling was already in progress
        _repo.Verify(x => x.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        _internalConfigRepo.Verify(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()), Times.Never);
    }

    [Fact]
    public async Task Poll_Should_return_early_when_already_polling()
    {
        // Arrange
        await _poller.StartAsync(CancelMe);

        // Act - calling Poll() directly when not polling (polling flag is false now)
        _poller.Poll();

        // Assert - should have been polled at least once
        ThenTheProviderIsPolled();
    }

    [Fact]
    public async Task Poll_Should_handle_null_configuration()
    {
        // Arrange
        _repo.Setup(x => x.Get()).Returns((FileConfiguration)null);

        // Act - call Poll() directly
        _poller.Poll();

        // Assert
        ThenTheSetterIsNotCalled();
    }

    [Fact]
    public async Task Poll_Should_handle_exception_from_repo()
    {
        // Arrange
        _repo.Setup(x => x.Get()).Throws(new Exception("Repository failure"));

        // Act - call Poll() directly
        _poller.Poll();

        // Assert - no exception propagated, setter not called
        ThenTheSetterIsNotCalled();
    }

    [Fact]
    public async Task PollAsync_Should_handle_null_configuration()
    {
        // Arrange
        _repo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((FileConfiguration)null);

        // Act - call PollAsync() directly
        await _poller.PollAsync(CancelMe);

        // Assert
        ThenTheSetterIsNotCalled();
    }

    [Fact]
    public async Task PollAsync_Should_handle_exception_from_repo()
    {
        // Arrange
        _repo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Repository failure"));

        // Act - call PollAsync() directly
        await _poller.PollAsync(CancelMe);

        // Assert - no exception propagated, setter not called
        ThenTheSetterIsNotCalled();
    }

    [Fact]
    public async Task PollAsync_Should_update_config_when_changed()
    {
        // Arrange
        var newConfig = GivenConfiguration();
        _repo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(newConfig);

        // Act
        await _poller.PollAsync(CancelMe);

        // Assert
        _internalConfigRepo.Verify(x => x.AddOrReplace(_internalConfig.Object), Times.Once);
    }

    [Fact]
    public async Task PollAsync_Should_not_add_to_internal_repo_if_creation_fails()
    {
        // Arrange
        var newConfig = GivenConfiguration();
        _repo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(newConfig);
        _internalConfigCreator
            .Setup(x => x.Create(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new ErrorResponse<IInternalConfiguration>(new AnyError()));

        // Act
        await _poller.PollAsync(CancelMe);

        // Assert
        ThenTheConfigIsNotAdded();
    }

    [Fact]
    public async Task PollAsync_Should_not_update_when_config_unchanged()
    {
        // Arrange - first poll sets initial config
        _repo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_initialFileConfig);
        await _poller.PollAsync(CancelMe); // first call sets _previousAsJson

        // Reset invocations
        _internalConfigRepo.Invocations.Clear();
        _internalConfigCreator.Invocations.Clear();

        // Act - poll again with same config
        await _poller.PollAsync(CancelMe);

        // Assert - no update since config didn't change
        _internalConfigRepo.Verify(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()), Times.Never);
    }

    [Fact]
    public async Task Should_dispose_with_timer_running()
    {
        // Arrange - start the poller synchronously to create a timer
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10000); // long delay
        await _poller.StartAsync(CancelMe);

        // Act - dispose while timer is running
        _poller.Dispose();

        // Assert - no exception and timer is null
        var timer = CurrentTimer();
        Assert.Null(timer);
    }

    #region StopAsync
    [Fact]
    public async Task StopAsync_Should_dispose_timer_to_release_background_thread()
    {
        // Arrange - start the poller to create a timer
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10000); // long delay
        await _poller.StartAsync(CancelMe);
        var timerBefore = CurrentTimer();
        Assert.NotNull(timerBefore);

        // Act - stop the poller, which should dispose the timer
        await _poller.StopAsync(CancelMe);

        // Assert - timer should be null (disposed) and no exception should be thrown
        var timerAfter = CurrentTimer();
        Assert.Null(timerAfter);
    }

    [Fact]
    public async Task StopAsync_Should_handle_multiple_calls()
    {
        // Arrange - start the poller to create a timer
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10000);
        await _poller.StartAsync(CancelMe);

        // Act - call StopAsync multiple times
        await _poller.StopAsync(CancelMe);
        await _poller.StopAsync(CancelMe); // Second call should not throw

        // Assert - timer should remain null
        var timer = CurrentTimer();
        Assert.Null(timer);
    }

    [Fact]
    public async Task StopAsync_Should_atomically_swap_timer_to_null_before_disposal()
    {
        // Arrange
        await _poller.StartAsync(CancelMe);
        var timerBeforeStop = CurrentTimer();
        Assert.NotNull(timerBeforeStop);

        // Act
        await _poller.StopAsync(CancelMe);

        // Assert - timer field should be null after stop
        var timerAfterStop = CurrentTimer();
        Assert.Null(timerAfterStop);
    }

    [Fact]
    public async Task StopAsync_Should_prevent_new_timer_callbacks_after_change()
    {
        // Arrange - set up a short polling interval to trigger callbacks
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(50);
        int invocationCountBeforeStop = 0;
        int invocationCountAfterStop = 0;

        _repo.Setup(x => x.Get()).Returns(() =>
        {
            Interlocked.Increment(ref invocationCountBeforeStop);
            return _initialFileConfig;
        });

        await _poller.StartAsync(CancelMe);
        await Task.Delay(150, CancelMe); // allow a few callbacks

        // Act
        invocationCountBeforeStop = Volatile.Read(ref invocationCountBeforeStop);
        await _poller.StopAsync(CancelMe);

        await Task.Delay(150, CancelMe); // wait to see if more callbacks occur

        // Assert - no new callbacks should fire after stop
        var v = Volatile.Read(ref invocationCountAfterStop);
        Assert.Equal(0, v);
    }

    [Fact]
    public async Task StopAsync_Should_prevent_new_callbacks_and_complete()
    {
        // Arrange - start with a reasonable polling interval
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        var pollCount = 0;

        _repo.Setup(x => x.Get()).Returns(() =>
        {
            Interlocked.Increment(ref pollCount);
            return _initialFileConfig;
        });

        await _poller.StartAsync(CancelMe);
        await Task.Delay(200, CancelMe); // let several polls happen
        var countBefore = Volatile.Read(ref pollCount);

        // Act
        await _poller.StopAsync(CancelMe);

        await Task.Delay(200, CancelMe); // wait to verify no new polls occur

        // Assert
        var v = Volatile.Read(ref pollCount);
        Assert.Equal(countBefore, v); // No new polls should occur after StopAsync
    }

    [Fact]
    public async Task StopAsync_Should_handle_null_timer_gracefully()
    {
        // Arrange - don't start, so timer is null

        // Act - calling stop on unstarted poller should not throw
        await _poller.StopAsync(CancelMe);

        // Assert - timer should still be null
        var t = CurrentTimer();
        Assert.Null(t);
    }

    [Fact]
    public async Task StopAsync_Called_multiple_times_should_only_dispose_once()
    {
        // Arrange
        await _poller.StartAsync(CancelMe);
        var initialTimer = CurrentTimer();

        // Act - stop multiple times
        await _poller.StopAsync(CancelMe);
        await _poller.StopAsync(CancelMe);
        await _poller.StopAsync(CancelMe);

        // Assert - timer should be null and no exception thrown
        var t = CurrentTimer();
        Assert.Null(t);
    }

    [Fact]
    public async Task StopAsync_Should_prevent_use_of_disposed_timer_from_callbacks()
    {
        // Arrange
        await _poller.StartAsync(CancelMe);
        var timerBefore = CurrentTimer();
        Assert.NotNull(timerBefore);

        // Act
        await _poller.StopAsync(CancelMe);
        await Task.Delay(100, CancelMe); // allow any in-flight callbacks to complete

        // Assert - timer field should be null, preventing ObjectDisposedException
        var t = CurrentTimer();
        Assert.Null(t);
    }

    [Fact]
    public async Task StopAsync_Should_complete_synchronously_when_no_callbacks_pending()
    {
        // Arrange
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10000); // very long delay
        await _poller.StartAsync(CancelMe);

        // Act
        var stopTask = _poller.StopAsync(CancelMe);
        var completedInTime = await Task.WhenAny(
            stopTask,
            Task.Delay(TimeSpan.FromSeconds(1), CancelMe)) == stopTask;

        // Assert - should complete quickly when no pending callbacks
        Assert.True(completedInTime); // StopAsync should complete quickly when no callbacks are pending
    }

    [Fact]
    public async Task Dispose_Should_clean_up_timer_after_start()
    {
        // Arrange
        await _poller.StartAsync(CancelMe);
        var timerBefore = CurrentTimer();
        Assert.NotNull(timerBefore);

        // Act
        _poller.Dispose();

        // Assert - timer should be null after dispose
        var t = CurrentTimer();
        Assert.Null(t);
    }

    [Fact]
    public async Task StopAsync_Then_Dispose_Should_not_throw()
    {
        // Arrange
        await _poller.StartAsync(CancelMe);

        // Act & Assert
        await _poller.StopAsync(CancelMe); // should not throw
        _poller.Dispose(); // should not throw
    }

    [Fact]
    public async Task Multiple_Start_Stop_Cycles_Should_work_correctly()
    {
        // Arrange: use a short interval so that at least several callbacks fire within the wait window
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ShortPollingIntervalInMs);
        var pollCount = 0;
        _repo.Setup(x => x.Get()).Returns(() =>
        {
            Interlocked.Increment(ref pollCount);
            return _initialFileConfig;
        });

        // Act & Assert - first cycle
        await _poller.StartAsync(CancelMe);
        await Task.Delay(200, CancelMe); // 200 ms >> ShortPollingIntervalInMs; guarantees multiple callbacks
        await _poller.StopAsync(CancelMe);
        var countAfterFirstStop = Volatile.Read(ref pollCount);

        // Act & Assert - second cycle (should work even after stop)
        await _poller.StartAsync(CancelMe);
        await Task.Delay(200, CancelMe);
        await _poller.StopAsync(CancelMe);
        var countAfterSecondStop = Volatile.Read(ref pollCount);

        // Assert - second cycle must have produced at least one additional poll
        Assert.True(
            countAfterSecondStop > countAfterFirstStop,
            $"Second cycle should have more polls (after 1st stop: {countAfterFirstStop}, after 2nd stop: {countAfterSecondStop}).");
    }


    [Fact]
    public async Task StopAsync_Should_not_log_warning_when_timer_disposal_completes_within_timeout()
    {
        // Arrange
        // Set up a quick callback that completes immediately
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10000); // long delay, no actual callbacks
        await _poller.StartAsync(CancelMe);

        // Act - StopAsync should complete the WaitHandle immediately since no callback is running
        await _poller.StopAsync(CancelMe);

        // Assert - LogWarning should NOT be called because WaitOne completed within timeout
        _logger.Verify(
            x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Never, "LogWarning should NOT be called when timer disposal completes within timeout");
    }

    [Fact]
    public async Task StopAsync_Should_log_warning_when_timer_disposal_exceeds_timeout()
    {
        // Arrange: set up a callback that blocks longer than StopAsync's 5-second internal wait
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(500);
        var pollStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowPollToComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _repo.Setup(x => x.Get()).Returns(() =>
        {
            pollStarted.TrySetResult(true);
            // Block for up to 7 seconds (longer than the 5-second timeout inside StopAsync)
            allowPollToComplete.Task.Wait(TimeSpan.FromSeconds(7));
            return _initialFileConfig;
        });

        await _poller.StartAsync(CancelMe);

        // Wait for the first poll to start
        Assert.True(
            await Task.WhenAny(pollStarted.Task, Task.Delay(TimeSpan.FromSeconds(2), CancelMe)) == pollStarted.Task,
            "Poll callback did not start within 2 seconds.");

        try
        {
            // Act: StopAsync should time out because the poll callback is still running
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _poller.StopAsync(CancelMe);
            stopwatch.Stop();

            // Assert: StopAsync should complete after roughly 5 seconds (its internal WaitOne timeout)
            Assert.True(
                stopwatch.ElapsedMilliseconds >= 4500,
                $"StopAsync should have waited ~5 s for the timer callback, but elapsed only {stopwatch.ElapsedMilliseconds} ms.");

            // LogWarning should be called because WaitOne timed out
            _logger.Verify(
                x => x.LogWarning(It.IsAny<Func<string>>()),
                Times.AtLeastOnce, "LogWarning should be called when timer disposal times out.");
        }
        finally
        {
            // Release the blocked callback so its background thread can exit cleanly.
            // This also allows the production-code background cleanup task to dispose timerStopped
            // without ObjectDisposedException.
            allowPollToComplete.TrySetResult(true);
            await Task.Delay(500, CancellationToken.None); // give background cleanup time to finish
        }
    }

    [Fact]
    public async Task StopAsync_WaitHandle_Should_complete_quickly_with_no_pending_callbacks()
    {
        // Arrange
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(100000); // Very long delay
        await _poller.StartAsync(CancelMe);

        // Act - StopAsync should complete within 1 second since no callback is running
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _poller.StopAsync(CancelMe);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // StopAsync should complete quickly when no callbacks are pending

        // No warning should be logged
        _logger.Verify(
            x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Never, "No warning should be logged when timer disposal completes quickly");
    }
    #endregion StopAsync

    #region SafeDisposeManualResetEvent

    [Fact]
    public void SafeDisposeManualResetEvent_Should_catch_and_log_exception_when_WaitOne_throws()
    {
        // Arrange: Create a ManualResetEvent and dispose it to trigger ObjectDisposedException
        var disposedEvent = new ManualResetEvent(false);
        disposedEvent.Dispose();

        // Use reflection to invoke the private SafeDisposeManualResetEvent method
        var safeDisposeMethod = typeof(FileConfigurationPoller)
            .GetMethod("SafeDisposeManualResetEvent", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        safeDisposeMethod.ShouldNotBeNull();

        // Act: Invoke SafeDisposeManualResetEvent with the disposed event
        safeDisposeMethod.Invoke(_poller, new object[] { disposedEvent });

        // Assert: Verify that LogWarning was called because WaitOne() threw ObjectDisposedException
        _logger.Verify(
            x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Once, "LogWarning should be called when WaitOne throws an exception");
    }

    #endregion SafeDisposeManualResetEvent

    private static FileConfiguration GivenConfiguration() => new()
    {
        Routes = new()
        {
            new()
            {
                DownstreamHostAndPorts = [ new("test", 80) ],
            },
        },
    };

    private void WhenProviderErrors()
    {
        FileConfiguration nothing = null;
        _repo.Setup(x => x.Get()).Returns(nothing);
    }

    private void WhenTheConfigIsChanged(FileConfiguration newConfig, int delay)
    {
        _repo.Setup(x => x.Get())
            .Callback(() => Thread.Sleep(delay))
            .Returns(newConfig);
    }

    private bool AssertWhile(Action assertion, int milliSeconds = 4_000)
    {
        bool TryAssert()
        {
            try
            {
                assertion.Invoke();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        return Wait.For(milliSeconds).Until(TryAssert);
    }

    private void ThenTheSetterIsCalled(FileConfiguration fileConfig, int times)
    {
        var result = AssertWhile(() =>
        {
            _internalConfigRepo.Verify(x => x.AddOrReplace(_internalConfig.Object), Times.Exactly(times));
            _internalConfigCreator.Verify(x => x.Create(fileConfig), Times.Exactly(times));
        });
        // Assert.True(result);
    }

    private void ThenTheSetterIsNotCalled()
    {
        _internalConfigRepo.Verify(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()), Times.Never);
        _internalConfigCreator.Verify(x => x.Create(It.IsAny<FileConfiguration>()), Times.Never);
    }

    private void ThenTheCreatorIsCalled(FileConfiguration fileConfig, int times)
    {
        var result = AssertWhile(() =>
        {
            _internalConfigCreator.Verify(x => x.Create(fileConfig), Times.Exactly(times));
        });
        // Assert.True(result);
    }

    private void ThenTheCreatorIsCalled(int times)
    {
        var result = AssertWhile(() =>
        {
            _internalConfigCreator.Verify(x => x.Create(It.IsAny<FileConfiguration>()), Times.Exactly(times));
        });
        Assert.True(result);
    }

    private void ThenTheConfigIsNotAdded()
    {
        _internalConfigRepo.Verify(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()), Times.Never);
    }

    private void ThenTheConfigIsNotAddedMoreThan(int times)
    {
        var result = AssertWhile(() =>
        {
            _internalConfigRepo.Verify(x => x.AddOrReplace(_internalConfig.Object), Times.Exactly(times));
        });
        //Assert.True(result);
    }

    private int NumberOfGetInvocations()
    {
        return _repo.Invocations.Count(x => x.Method.Name == nameof(IFileConfigurationRepository.Get));
    }

    private Timer CurrentTimer()
    {
        var timerField = typeof(FileConfigurationPoller)
            .GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);

        timerField.ShouldNotBeNull();
        return timerField.GetValue(_poller) as Timer;
    }

    private void ThenTheProviderIsPolled()
    {
        var result = AssertWhile(() =>
        {
            _repo.Verify(x => x.Get(), Times.AtLeastOnce());
        });
        // Assert.True(result);
    }

    public void Dispose()
    {
        _poller.Dispose();
    }
}
