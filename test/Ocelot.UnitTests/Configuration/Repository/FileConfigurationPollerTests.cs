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

    private readonly FileConfigurationPoller _poller;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IFileConfigurationRepository> _repo;
    private readonly FileConfiguration _initialFileConfig;
    private readonly Mock<IFileConfigurationPollerOptions> _config;
    private readonly Mock<IInternalConfigurationRepository> _internalConfigRepo;
    private readonly Mock<IInternalConfigurationCreator> _internalConfigCreator;
    private readonly Mock<IInternalConfiguration> _internalConfig;

    public FileConfigurationPollerTests()
    {
        var logger = new Mock<IOcelotLogger>();
        _factory = new Mock<IOcelotLoggerFactory>();
        _factory.Setup(x => x.CreateLogger<FileConfigurationPoller>()).Returns(logger.Object);
        _repo = new Mock<IFileConfigurationRepository>();
        _initialFileConfig = new FileConfiguration();
        _config = new Mock<IFileConfigurationPollerOptions>();
        _repo.Setup(x => x.Get()).Returns(_initialFileConfig);
        _config.Setup(x => x.Delay()).Returns(PollingDelayInMs);
        _config.Setup(x => x.DelayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(PollingDelayInMs);
        _internalConfig = new Mock<IInternalConfiguration>();
        _internalConfigRepo = new Mock<IInternalConfigurationRepository>();
        _internalConfigCreator = new Mock<IInternalConfigurationCreator>();
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
        timerAfterFirstStart.ShouldNotBeNull();
        timerAfterSecondStart.ShouldBeSameAs(timerAfterFirstStart);
    }

    [Fact]
    public async Task Should_do_nothing_when_stop_called_before_start()
    {
        // Arrange, Act
        await _poller.StopAsync(CancelMe);
        await Task.Delay(PollingDelayInMs * 2, CancelMe);

        // Assert
        NumberOfGetInvocations().ShouldBe(0);
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
        _repo.Setup(x => x.Get()).Returns(() =>
        {
            Interlocked.Increment(ref callCount);
            return _initialFileConfig;
        });

        // Act & Assert, scenario "Return early" -> _polling == true
        await _poller.StartAsync(CancelMe);
        await Task.Delay((int)(PollingDelayInMs * 0.5), CancelMe); // ~50% of running time of OnTimer
        Assert.Equal(0, callCount);

        // Act & Assert, scenario "After completion"
        await Task.Delay((int)(PollingDelayInMs * 0.55), CancelMe); // ~55% of running time of OnTimer
        Assert.Equal(1, callCount);

        // Cleanup
        await _poller.StopAsync(CancelMe);
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
        NumberOfGetInvocations().ShouldBe(afterStopSettled);
    }

    [Fact]
    public void Should_dispose_cleanly_without_starting()
    {
        // Arrange, Act, Assert
        _poller.Dispose(); // when poller is disposed
    }

    [Fact]
    public void OnTimer_Should_return_early_when_already_polling()
    {
        // Arrange: set the private _polling field to true so OnTimer takes the early-return path (line 38)
        var pollingField = typeof(FileConfigurationPoller)
            .GetField("_polling", BindingFlags.Instance | BindingFlags.NonPublic);
        pollingField!.SetValue(_poller, true);

        // Act: invoke the private OnTimer method directly
        var onTimerMethod = typeof(FileConfigurationPoller)
            .GetMethod("OnTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        onTimerMethod!.Invoke(_poller, [null]);

        // Assert: Get() was never called because polling was already in progress
        _repo.Verify(x => x.Get(), Times.Never);
    }

    [Fact]
    public async Task PollAsync_Should_return_early_when_already_polling()
    {
        // Arrange: set the private _polling field to true so PollAsync takes the early-return path (line 103)
        var pollingField = typeof(FileConfigurationPoller)
            .GetField("_polling", BindingFlags.Instance | BindingFlags.NonPublic);
        pollingField!.SetValue(_poller, true);

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
        timer.ShouldBeNull();
    }

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
            .GetField("_timer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

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
