using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Configuration;

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
        var newConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new("test", 80),
                    },
                },
            },
        };

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
        var newConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new("test", 80),
                    },
                },
            },
        };

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
        var newConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new("test", 80),
                    },
                },
            },
        };

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

    private void ThenTheSetterIsCalled(FileConfiguration fileConfig, int times)
    {
        var result = Wait.For(4_000).Until(() =>
        {
            try
            {
                _internalConfigRepo.Verify(x => x.AddOrReplace(_internalConfig.Object), Times.Exactly(times));
                _internalConfigCreator.Verify(x => x.Create(fileConfig), Times.Exactly(times));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    private void ThenTheSetterIsCalledAtLeast(FileConfiguration fileConfig, int times)
    {
        var result = Wait.For(4_000).Until(() =>
        {
            try
            {
                _internalConfigRepo.Verify(x => x.AddOrReplace(_internalConfig.Object), Times.AtLeast(times));
                _internalConfigCreator.Verify(x => x.Create(fileConfig), Times.AtLeast(times));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    public void Dispose()
    {
        _poller.Dispose();
    }
}
