using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

[Trait("Feat", "2058")]
[Trait("Bug", "2059")]
public class CacheOptionsCreatorTests
{
    [Fact]
    public void ShouldCreateCacheOptions()
    {
        // Arrange
        var options = FileCacheOptionsFactory();
        var cacheOptionsCreator = new CacheOptionsCreator();

        // Act
        var result = cacheOptionsCreator.Create(options, null, null, null);

        // Assert
        result.TtlSeconds.ShouldBe(options.TtlSeconds.Value);
        result.Region.ShouldBe(options.Region);
        result.Header.ShouldBe(options.Header);
        result.EnableContentHashing.ShouldBe(options.EnableContentHashing.Value);
    }

    [Fact]
    public void ShouldCreateCacheOptionsUsingGlobalConfiguration()
    {
        // Arrange
        var global = GlobalConfigurationFactory();
        var cacheOptionsCreator = new CacheOptionsCreator();

        // Act
        var result = cacheOptionsCreator.Create(new FileCacheOptions(), global, null, null);

        // Assert
        result.TtlSeconds.ShouldBe(global.CacheOptions.TtlSeconds.Value);
        result.Region.ShouldBe(global.CacheOptions.Region);
        result.Header.ShouldBe(global.CacheOptions.Header);
        result.EnableContentHashing.ShouldBe(global.CacheOptions.EnableContentHashing.Value);
    }

    [Fact]
    public void RouteCacheOptionsShouldOverrideGlobalConfiguration()
    {
        // Arrange
        var global = GlobalConfigurationFactory();
        var options = FileCacheOptionsFactory();
        var cacheOptionsCreator = new CacheOptionsCreator();

        // Act
        var result = cacheOptionsCreator.Create(options, global, null, null);

        // Assert
        result.TtlSeconds.ShouldBe(options.TtlSeconds.Value);
        result.Region.ShouldBe(options.Region);
        result.Header.ShouldBe(options.Header);
        result.EnableContentHashing.ShouldBe(options.EnableContentHashing.Value);
    }

    [Fact]
    public void ShouldCreateCacheOptionsWithDefaults()
    {
        // Arrange
        var cacheOptionsCreator = new CacheOptionsCreator();

        // Act
        var result = cacheOptionsCreator.Create(new FileCacheOptions(), null, "/", new List<string> { "GET" });

        // Assert
        result.TtlSeconds.ShouldBe(0);
        result.Region.ShouldBe("GET");
        result.Header.ShouldBe(null);
        result.EnableContentHashing.ShouldBe(false);
    }

    [Fact]
    public void ShouldComputeRegionIfNotProvided()
    {
        // Arrange
        var global = GlobalConfigurationFactory();
        var options = FileCacheOptionsFactory();
        global.CacheOptions.Region = null;
        options.Region = null;
        var cacheOptionsCreator = new CacheOptionsCreator();

        // Act
        var result = cacheOptionsCreator.Create(options, global, "/api/values", new List<string> { "GET", "POST" });

        // Assert
        result.TtlSeconds.ShouldBe(options.TtlSeconds.Value);
        result.Region.ShouldBe("GETPOSTapivalues");
        result.Header.ShouldBe(options.Header);
        result.EnableContentHashing.ShouldBe(options.EnableContentHashing.Value);
    }

    private static FileGlobalConfiguration GlobalConfigurationFactory() => new()
    {
        CacheOptions = new FileCacheOptions
        {
            TtlSeconds = 20,
            Region = "globalRegion",
            Header = "globalHeader",
            EnableContentHashing = false,
        },
    };

    private static FileCacheOptions FileCacheOptionsFactory() => new()
    {
        TtlSeconds = 10,
        Region = "region",
        Header = "header",
        EnableContentHashing = true,
    };
}
