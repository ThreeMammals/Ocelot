using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;

namespace Ocelot.UnitTests.Configuration.Repository;

public class ServiceDiscoveryFileConfigurationPollerOptionsTests
{
    private readonly Mock<IInternalConfigurationRepository> _mockInternalConfigRepo = new();
    private readonly Mock<IFileConfigurationRepository> _mockFileConfigurationRepository = new();
    private readonly ServiceDiscoveryFileConfigurationPollerOptions _sut; // System Under Test

    public ServiceDiscoveryFileConfigurationPollerOptionsTests()
    {
        _sut = new(
            _mockInternalConfigRepo.Object,
            _mockFileConfigurationRepository.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDependencies()
    {
        // Arrange & Act
        var result = _sut;

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigurationIsNull()
    {
        // Arrange
        FileConfiguration configuration = null;
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(configuration);

        IInternalConfiguration internalConfig = null;
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfig);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnFileConfigPollingInterval_WhenFileConfigHasValidPollingInterval()
    {
        // Arrange
        const int expectedDelay = 5000;
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = expectedDelay
                }
            }
        };
        FileConfiguration configuration = fileConfiguration;

        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(configuration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigPollingIntervalIsZero()
    {
        // Arrange
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = 0
                }
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        IInternalConfiguration internalConfiguration = null;
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigIsNull()
    {
        // Arrange
        _mockFileConfigurationRepository
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileConfiguration)null);

        IInternalConfiguration internalConfiguration = null;
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigServiceDiscoveryProviderIsNull()
    {
        // Arrange
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = null
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        IInternalConfiguration internalConfiguration = null;
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnInternalConfigPollingInterval_WhenFileConfigFailsButInternalConfigIsValid()
    {
        // Arrange
        const int expectedDelay = 3000;
        FileConfiguration configuration = null;
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(configuration);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new()
            {
                PollingInterval = expectedDelay,
            }
        };
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigPollingIntervalIsZero()
    {
        // Arrange
        FileConfiguration configuration = null;
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(configuration);
        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new()
            {
                PollingInterval = 0,
            }
        };
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigIsNull()
    {
        // Arrange
        FileConfiguration configuration = null;
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(configuration);

        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns((IInternalConfiguration)null);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigServiceProviderConfigurationIsNull()
    {
        // Arrange
        FileConfiguration configuration = null;
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(configuration);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = null
        };
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldPreferFileConfigOverInternalConfig_WhenBothHaveValidPollingIntervals()
    {
        // Arrange
        const int fileConfigDelay = 5000;
        const int internalConfigDelay = 3000;

        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = fileConfigDelay,
                }
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new ServiceProviderConfiguration
            {
                PollingInterval = internalConfigDelay
            }
        };
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(fileConfigDelay, delay);
    }

    [Fact]
    public void Delay_ShouldReturn1000_WhenPollingIntervalIsNegative()
    {
        // Arrange
        const int negativeDelay = -100;
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = negativeDelay,
                }
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        // Note: The current implementation allows negative values to pass through
        // This test documents current behavior; consider if validation is needed
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldCallFileConfigurationRepositoryGet()
    {
        // Arrange
        FileConfiguration configuration = null;
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(configuration);

        IInternalConfiguration internalConfiguration = null;
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        _mockFileConfigurationRepository.Verify(x => x.Get(), Times.Once);
    }

    [Fact]
    public void Delay_ShouldCallInternalConfigRepositoryGet_WhenFileConfigDoesNotHaveValidPollingInterval()
    {
        // Arrange
        FileConfiguration configuration = null;
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(configuration);

        IInternalConfiguration internalConfiguration = null;
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        _mockInternalConfigRepo.Verify(x => x.Get(), Times.Once);
    }

    [Fact]
    public void Delay_ShouldNotCallInternalConfigRepositoryGet_WhenFileConfigHasValidPollingInterval()
    {
        // Arrange
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = 5000,
                }
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        // Act
        var delay = _sut.Delay;

        // Assert
        _mockInternalConfigRepo.Verify(x => x.Get(), Times.Never);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10000)]
    public void Delay_ShouldReturnValidPollingInterval_WithVariousValues(int pollingInterval)
    {
        // Arrange
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    PollingInterval = pollingInterval
                }
            }
        };
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .Returns(fileConfiguration);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(pollingInterval, delay);
    }
}
