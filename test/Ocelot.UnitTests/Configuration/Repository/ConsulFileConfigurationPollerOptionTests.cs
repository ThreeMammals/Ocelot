using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Provider.Consul;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Configuration.Repository;

public class ConsulFileConfigurationPollerOptionTests
{
    private readonly Mock<IInternalConfigurationRepository> _mockInternalConfigRepo = new();
    private readonly Mock<IFileConfigurationRepository> _mockFileConfigurationRepository = new();
    private readonly ConsulFileConfigurationPollerOption _sut; // System Under Test

    public ConsulFileConfigurationPollerOptionTests()
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
        var fileConfigResponse = new OkResponse<FileConfiguration>(null);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfigResponse = new OkResponse<IInternalConfiguration>(null);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

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
        var fileConfigResponse = new OkResponse<FileConfiguration>(fileConfiguration);

        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        // Act
        var delay = _sut.Delay;

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
        var fileConfigResponse = new OkResponse<FileConfiguration>(fileConfiguration);

        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfigResponse = new OkResponse<IInternalConfiguration>(null);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenFileConfigIsError()
    {
        // Arrange
        var err = new UnableToSetConfigInConsulError("Error message");
        var fileConfigResponse = new ErrorResponse<FileConfiguration>(err);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfigResponse = new OkResponse<IInternalConfiguration>(null);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

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
        var fileConfigResponse = new OkResponse<FileConfiguration>(fileConfiguration);

        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfigResponse = new OkResponse<IInternalConfiguration>(null);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnInternalConfigPollingInterval_WhenFileConfigFailsButInternalConfigIsValid()
    {
        // Arrange
        const int expectedDelay = 3000;
        var fileConfigResponse = new OkResponse<FileConfiguration>(null);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new()
            {
                PollingInterval = expectedDelay,
            }
        };
        var internalConfigResponse = new OkResponse<IInternalConfiguration>(internalConfiguration);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

        // Assert
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigPollingIntervalIsZero()
    {
        // Arrange
        var fileConfigResponse = new OkResponse<FileConfiguration>(null);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new()
            {
                PollingInterval = 0,
            }
        };
        var internalConfigResponse = new OkResponse<IInternalConfiguration>(internalConfiguration);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigIsError()
    {
        // Arrange
        var fileConfigResponse = new OkResponse<FileConfiguration>(null);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var err = new UnableToSetConfigInConsulError("Error message");
        var internalConfigResponse = new ErrorResponse<IInternalConfiguration>(err);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

        // Assert
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldReturnDefaultValue_WhenInternalConfigServiceProviderConfigurationIsNull()
    {
        // Arrange
        var fileConfigResponse = new OkResponse<FileConfiguration>(null);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = null
        };
        var internalConfigResponse = new OkResponse<IInternalConfiguration>(internalConfiguration);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

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
        var fileConfigResponse = new OkResponse<FileConfiguration>(fileConfiguration);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfiguration = new InternalConfiguration
        {
            ServiceProviderConfiguration = new ServiceProviderConfiguration
            {
                PollingInterval = internalConfigDelay
            }
        };
        var internalConfigResponse = new OkResponse<IInternalConfiguration>(internalConfiguration);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

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
        var fileConfigResponse = new OkResponse<FileConfiguration>(fileConfiguration);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        // Act
        var delay = _sut.Delay;

        // Assert
        // Note: The current implementation allows negative values to pass through
        // This test documents current behavior; consider if validation is needed
        Assert.Equal(1000, delay);
    }

    [Fact]
    public void Delay_ShouldCallFileConfigurationRepositoryGet()
    {
        // Arrange
        var fileConfigResponse = new OkResponse<FileConfiguration>(null);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfigResponse = new OkResponse<IInternalConfiguration>(null);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

        // Assert
        _mockFileConfigurationRepository.Verify(x => x.Get(), Times.Once);
    }

    [Fact]
    public void Delay_ShouldCallInternalConfigRepositoryGet_WhenFileConfigDoesNotHaveValidPollingInterval()
    {
        // Arrange
        var fileConfigResponse = new OkResponse<FileConfiguration>(null);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        var internalConfigResponse = new OkResponse<IInternalConfiguration>(null);
        _mockInternalConfigRepo
            .Setup(x => x.Get())
            .Returns(internalConfigResponse);

        // Act
        var delay = _sut.Delay;

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
        var fileConfigResponse = new OkResponse<FileConfiguration>(fileConfiguration);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

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
        var fileConfigResponse = new OkResponse<FileConfiguration>(fileConfiguration);
        _mockFileConfigurationRepository
            .Setup(x => x.Get())
            .ReturnsAsync(fileConfigResponse);

        // Act
        var delay = _sut.Delay;

        // Assert
        Assert.Equal(pollingInterval, delay);
    }
}
