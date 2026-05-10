using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;

namespace Ocelot.UnitTests.Configuration.Repository;

public class ServiceDiscoveryFileConfigurationPollerOptionsTests
{
    private readonly Mock<IInternalConfigurationRepository> _internalRepo = new();
    private readonly Mock<IFileConfigurationRepository> _fileRepo = new();
    private readonly ServiceDiscoveryFileConfigurationPollerOptions _sut;
    private static CancellationToken CancelMe => TestContext.Current.CancellationToken;

    public ServiceDiscoveryFileConfigurationPollerOptionsTests()
    {
        _sut = new(_internalRepo.Object, _fileRepo.Object);
    }

    [Fact]
    public async Task DelayAsync_ShouldReturnPollingInterval_WhenFileConfigHasValidPollingInterval()
    {
        // Arrange
        const int expectedDelay = 5000;
        var fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new() { PollingInterval = expectedDelay }
            }
        };
        _fileRepo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fileConfiguration);

        // Act
        var delay = await _sut.DelayAsync(CancelMe);

        // Assert
        Assert.Equal(expectedDelay, delay);
        _fileRepo.Verify(x => x.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DelayAsync_ShouldReturnDefaultDelay_WhenFileConfigIsNull()
    {
        // Arrange
        _fileRepo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((FileConfiguration)null);
        _internalRepo.Setup(x => x.Get()).Returns((IInternalConfiguration)null);

        // Act
        var delay = await _sut.DelayAsync(CancelMe);

        // Assert
        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, delay);
        _fileRepo.Verify(x => x.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    public static TheoryData<FileConfiguration, IInternalConfiguration, int> GetDelayTestCases => new()
    {
        // Branch 1: file config has a valid polling interval → file config value wins
        {
            new FileConfiguration
            {
                GlobalConfiguration = new() { ServiceDiscoveryProvider = new() { PollingInterval = 5000 } }
            },
            null,
            5000
        },

        // Branch 2: file config polling interval is zero → fall through to internal config
        {
            new FileConfiguration
            {
                GlobalConfiguration = new() { ServiceDiscoveryProvider = new() { PollingInterval = 0 } }
            },
            new InternalConfiguration { ServiceProviderConfiguration = new ServiceProviderConfiguration { PollingInterval = 3000 } },
            3000
        },

        // Branch 3: file config is null, internal config has valid polling interval
        {
            null,
            new InternalConfiguration { ServiceProviderConfiguration = new ServiceProviderConfiguration { PollingInterval = 2000 } },
            2000
        },

        // Branch 4 (fallback): file config is null, internal config is null → default delay
        {
            null,
            null,
            ServiceDiscoveryFileConfigurationPollerOptions.DefaultDelayMilliseconds
        },

        // Branch 5 (fallback): both polling intervals are zero → default delay
        {
            new FileConfiguration
            {
                GlobalConfiguration = new() { ServiceDiscoveryProvider = new() { PollingInterval = 0 } }
            },
            new InternalConfiguration { ServiceProviderConfiguration = new ServiceProviderConfiguration { PollingInterval = 0 } },
            ServiceDiscoveryFileConfigurationPollerOptions.DefaultDelayMilliseconds
        },

        // Branch 6 (fallback): file config has null service discovery provider, internal config is null
        {
            new FileConfiguration { GlobalConfiguration = new() { ServiceDiscoveryProvider = null } },
            null,
            ServiceDiscoveryFileConfigurationPollerOptions.DefaultDelayMilliseconds
        },

        // Branch 7 (fallback): file config has null service discovery provider, internal config is null
        {
            new FileConfiguration { GlobalConfiguration = null },
            null,
            ServiceDiscoveryFileConfigurationPollerOptions.DefaultDelayMilliseconds
        },
    };

    [Theory]
    [MemberData(nameof(GetDelayTestCases))]
    public void GetDelay_ShouldReturn_CorrectInterval_ForAllBranches(
        FileConfiguration fileConfig,
        IInternalConfiguration internalConfig,
        int expectedDelay)
    {
        // Arrange
        _fileRepo.Setup(x => x.Get()).Returns(fileConfig);
        _internalRepo.Setup(x => x.Get()).Returns(internalConfig);

        // Act
        var delay = _sut.Delay();

        // Assert
        Assert.Equal(expectedDelay, delay);
    }
}
