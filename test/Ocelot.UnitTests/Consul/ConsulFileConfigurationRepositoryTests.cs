using Consul;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ocelot.Cache;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.Responses;
using System.Text;

namespace Ocelot.UnitTests.Consul;

public class ConsulFileConfigurationRepositoryTests : UnitTest
{
    private ConsulFileConfigurationRepository _repo;
    private readonly Mock<IOptions<FileConfiguration>> _options;
    private readonly Mock<IOcelotCache<FileConfiguration>> _cache;
    private readonly Mock<IConsulClientFactory> _factory;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IConsulClient> _client;
    private readonly Mock<IKVEndpoint> _kvEndpoint;
    private FileConfiguration _fileConfiguration;
    private Response<FileConfiguration> _getResult;

    public ConsulFileConfigurationRepositoryTests()
    {
        _cache = new Mock<IOcelotCache<FileConfiguration>>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();

        _options = new Mock<IOptions<FileConfiguration>>();
        _factory = new Mock<IConsulClientFactory>();
        _client = new Mock<IConsulClient>();
        _kvEndpoint = new Mock<IKVEndpoint>();
        _client
            .Setup(x => x.KV)
            .Returns(_kvEndpoint.Object);
        _factory
            .Setup(x => x.Get(It.IsAny<ConsulRegistryConfiguration>()))
            .Returns(_client.Object);
        _options
            .SetupGet(x => x.Value)
            .Returns(() => _fileConfiguration);
    }

    [Fact]
    public async Task Should_set_config()
    {
        // Arrange
        var config = GivenFakeFileConfiguration();
        GivenWritingToConsulSucceeds();

        // Act
        _ = await _repo.Set(config);

        // Assert
        ThenTheConfigurationIsStoredAs(config);
    }

    [Fact]
    public async Task Should_get_config()
    {
        // Arrange
        var config = _fileConfiguration = GivenFakeFileConfiguration();
        GivenFetchFromConsulSucceeds();

        // Act
        _getResult = await _repo.Get();

        // Assert
        ThenTheConfigurationIs(config);
    }

    [Fact]
    public async Task Should_get_null_config()
    {
        // Arrange
        _fileConfiguration = GivenFakeFileConfiguration();
        GivenFetchFromConsulReturnsNull();

        // Act
        _getResult = await _repo.Get();

        // Assert
        _getResult.Data.ShouldBeNull();
    }

    [Fact]
    public async Task Should_get_config_from_cache()
    {
        // Arrange
        var config = _fileConfiguration = GivenFakeFileConfiguration();
        GivenFetchFromCacheSucceeds();

        // Act
        _getResult = await _repo.Get();

        // Assert
        ThenTheConfigurationIs(config);
    }

    [Fact]
    public async Task Should_set_config_key()
    {
        // Arrange
        _fileConfiguration = GivenFakeFileConfiguration();
        GivenTheConfigKeyComesFromFileConfig("Tom");
        GivenFetchFromConsulSucceeds();

        // Act
        _getResult = await _repo.Get();

        // Assert
        ThenTheConfigKeyIs("Tom");
    }

    [Fact]
    public async Task Should_set_default_config_key()
    {
        // Arrange
        _fileConfiguration = GivenFakeFileConfiguration();
        GivenFetchFromConsulSucceeds();

        // Act
        _getResult = await _repo.Get();

        // Assert
        ThenTheConfigKeyIs("InternalConfiguration");
    }

    private void ThenTheConfigKeyIs(string expected)
    {
        _kvEndpoint.Verify(x => x.Get(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void GivenTheConfigKeyComesFromFileConfig(string key)
    {
        _fileConfiguration.GlobalConfiguration.ServiceDiscoveryProvider.ConfigurationKey = key;
        _repo = new ConsulFileConfigurationRepository(_options.Object, _cache.Object, _factory.Object, _loggerFactory.Object);
    }

    private void ThenTheConfigurationIs(FileConfiguration config)
    {
        var expected = JsonConvert.SerializeObject(config, Formatting.Indented);
        var result = JsonConvert.SerializeObject(_getResult.Data, Formatting.Indented);
        result.ShouldBe(expected);
    }

    private void GivenWritingToConsulSucceeds()
    {
        var response = new WriteResult<bool>
        {
            Response = true,
        };
        _kvEndpoint.Setup(x => x.Put(It.IsAny<KVPair>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    private void GivenFetchFromCacheSucceeds()
    {
        _cache.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<string>())).Returns(_fileConfiguration);
    }

    private void GivenFetchFromConsulReturnsNull()
    {
        var result = new QueryResult<KVPair>();
        _kvEndpoint.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private void GivenFetchFromConsulSucceeds()
    {
        var json = JsonConvert.SerializeObject(_fileConfiguration, Formatting.Indented);
        var bytes = Encoding.UTF8.GetBytes(json);
        var kvp = new KVPair("OcelotConfiguration")
        {
            Value = bytes,
        };
        var query = new QueryResult<KVPair>
        {
            Response = kvp,
        };
        _kvEndpoint.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(query);
    }

    private void ThenTheConfigurationIsStoredAs(FileConfiguration config)
    {
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        var bytes = Encoding.UTF8.GetBytes(json);
        _kvEndpoint.Verify(x => x.Put(It.Is<KVPair>(k => k.Value.SequenceEqual(bytes)), It.IsAny<CancellationToken>()), Times.Once);
    }

    private FileConfiguration GivenFakeFileConfiguration()
    {
        var routes = new List<FileRoute>
        {
            new()
            {
                DownstreamHostAndPorts = new List<FileHostAndPort>
                {
                    new("123.12.12.12", 80),
                },
                DownstreamScheme = Uri.UriSchemeHttps,
                DownstreamPathTemplate = "/asdfs/test/{test}",
            },
        };
        var globalConfiguration = new FileGlobalConfiguration
        {
            ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
            {
                Scheme = Uri.UriSchemeHttps,
                Port = 198,
                Host = "blah",
            },
        };
        _fileConfiguration = new FileConfiguration
        {
            GlobalConfiguration = globalConfiguration,
            Routes = routes,
        };
        _repo = new ConsulFileConfigurationRepository(_options.Object, _cache.Object, _factory.Object, _loggerFactory.Object);
        return _fileConfiguration;
    }
}
