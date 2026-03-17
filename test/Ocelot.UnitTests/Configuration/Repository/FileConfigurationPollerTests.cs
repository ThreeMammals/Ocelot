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
    private readonly FileConfigurationPoller _poller;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IFileConfigurationRepository> _repo;
    private readonly FileConfiguration _fileConfig;
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
        _fileConfig = new FileConfiguration();
        _config = new Mock<IFileConfigurationPollerOptions>();
        _repo.Setup(x => x.Get()).ReturnsAsync(new OkResponse<FileConfiguration>(_fileConfig));
        _config.Setup(x => x.Delay).Returns(100);
        _internalConfig = new Mock<IInternalConfiguration>();
        _internalConfigRepo = new Mock<IInternalConfigurationRepository>();
        _internalConfigCreator = new Mock<IInternalConfigurationCreator>();
        _internalConfigCreator.Setup(x => x.Create(It.IsAny<FileConfiguration>())).ReturnsAsync(new OkResponse<IInternalConfiguration>(_internalConfig.Object));
        _poller = new FileConfigurationPoller(_factory.Object, _repo.Object, _config.Object, _internalConfigRepo.Object, _internalConfigCreator.Object);
    }

    [Fact]
    public void Should_start()
    {
        // Arrange, Act
        _poller.StartAsync(CancellationToken.None);

        // Assert
        ThenTheSetterIsCalled(_fileConfig, 1);
    }

    [Fact]
    public void Should_call_setter_when_gets_new_config()
    {
        // Arrange
        var newConfig = GivenConfiguration();

        // Act
        _poller.StartAsync(CancellationToken.None);

        // Assert
        WhenTheConfigIsChanged(newConfig, 0);
        ThenTheSetterIsCalledAtLeast(newConfig, 1);
    }

    [Fact]
    public void Should_not_poll_if_already_polling()
    {
        // Arrange
        var newConfig = GivenConfiguration();

        // Act
        _poller.StartAsync(CancellationToken.None);

        // Assert
        WhenTheConfigIsChanged(newConfig, 10);
        ThenTheSetterIsCalled(newConfig, 1);
    }

    [Fact]
    public void Should_do_nothing_if_call_to_provider_fails()
    {
        // Arrange
        var newConfig = GivenConfiguration();

        // Act
        _poller.StartAsync(CancellationToken.None);
        WhenProviderErrors();

        // Assert
        ThenTheSetterIsCalled(newConfig, 0);
    }

    [Fact]
    public void Should_dispose_cleanly_without_starting()
    {
        // Arrange, Act, Assert
        _poller.Dispose(); // when poller is disposed
    }

    [Fact]
    public async Task StopAsync()
    {
        // Arrange
        await _poller.StartAsync(CancellationToken.None);

        // Act
        await _poller.StopAsync(CancellationToken.None);

        // Assert: no exception is thrown and the poller stops cleanly
    }

    /*[Fact]
    public async Task Should_not_update_internal_config_if_internal_config_creator_errors()
    {
        // Arrange
        _internalConfigCreator
            .Setup(x => x.Create(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new ErrorResponse<IInternalConfiguration>(new AnyError()));

        // Act
        await _poller.StartAsync(CancellationToken.None);

        // Assert: Create is called but AddOrReplace is never called when the creator returns an error
        var result = AssertWhile(() => 
        {
            _internalConfigCreator.Verify(x => x.Create(_fileConfig), Times.AtLeastOnce);
            _internalConfigRepo.Verify(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()), Times.Never);
        });
        Assert.True(result);
    }*/

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
        _repo
            .Setup(x => x.Get())
            .ReturnsAsync(new ErrorResponse<FileConfiguration>(new AnyError()));
    }

    private void WhenTheConfigIsChanged(FileConfiguration newConfig, int delay)
    {
        _repo
            .Setup(x => x.Get())
            .Callback(() => Thread.Sleep(delay))
            .ReturnsAsync(new OkResponse<FileConfiguration>(newConfig));
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
        Assert.True(result);
    }

    private void ThenTheSetterIsCalledAtLeast(FileConfiguration fileConfig, int times)
    {
        var result = AssertWhile(() =>
        {
            _internalConfigRepo.Verify(x => x.AddOrReplace(_internalConfig.Object), Times.AtLeast(times));
            _internalConfigCreator.Verify(x => x.Create(fileConfig), Times.AtLeast(times));
        });
        Assert.True(result);
    }

    public void Dispose()
    {
        _poller.Dispose();
    }
}
