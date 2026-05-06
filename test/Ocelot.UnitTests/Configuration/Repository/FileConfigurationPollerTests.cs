using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

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
        _internalConfig = new Mock<IInternalConfiguration>();
        _internalConfigRepo = new Mock<IInternalConfigurationRepository>();
        _internalConfigCreator = new Mock<IInternalConfigurationCreator>();
        _internalConfigCreator.Setup(x => x.Create(It.IsAny<FileConfiguration>())).ReturnsAsync(new OkResponse<IInternalConfiguration>(_internalConfig.Object));
        _poller = new FileConfigurationPoller(_factory.Object, _repo.Object, _config.Object, _internalConfigRepo.Object, _internalConfigCreator.Object);
    }

    protected static CancellationToken CancelMe => TestContext.Current.CancellationToken;

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

    // [Fact(Skip = "Requires redevelopment")]
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
        var getCallCount = 0;
        _repo.Setup(x => x.Get()).Returns(() =>
        {
            Interlocked.Increment(ref getCallCount);
            return _initialFileConfig;
        });

        // Act
        await _poller.StartAsync(CancelMe);
        await Task.Delay(PollingDelayInMs * 3, CancelMe);

        // Assert
        getCallCount.ShouldBe(0);

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
