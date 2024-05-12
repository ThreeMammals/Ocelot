using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class CacheOptionsCreatorTests
{
    [Fact]
    [Trait("Issue", "2059")]
    public void ShouldCreateCacheOptions()
    {
        var fileCacheOptions = FileCacheOptionsFactory();
        var cacheOptionsCreator = new CacheOptionsCreator();
        var result = cacheOptionsCreator.Create(fileCacheOptions, null, null, null);

        result.TtlSeconds.ShouldBe(fileCacheOptions.TtlSeconds.Value);
        result.Region.ShouldBe(fileCacheOptions.Region);
        result.Header.ShouldBe(fileCacheOptions.Header);
        result.EnableContentHashing.ShouldBe(fileCacheOptions.EnableContentHashing.Value);
    }

    [Fact]
    [Trait("Issue", "2059")]
    public void ShouldCreateCacheOptionsUsingGlobalConfiguration()
    {
        var globalConfiguration = GlobalConfigurationFactory();
        var fileCacheOptions = new FileCacheOptions();

        var cacheOptionsCreator = new CacheOptionsCreator();
        var result = cacheOptionsCreator.Create(fileCacheOptions, null, null, globalConfiguration);

        result.TtlSeconds.ShouldBe(globalConfiguration.CacheOptions.TtlSeconds.Value);
        result.Region.ShouldBe(globalConfiguration.CacheOptions.Region);
        result.Header.ShouldBe(globalConfiguration.CacheOptions.Header);
        result.EnableContentHashing.ShouldBe(globalConfiguration.CacheOptions.EnableContentHashing.Value);
    }

    [Fact]
    [Trait("Issue", "2059")]
    public void RouteCacheOptionsShouldOverrideGlobalConfiguration()
    {
        var globalConfiguration = GlobalConfigurationFactory();
        var fileCacheOptions = FileCacheOptionsFactory();

        var cacheOptionsCreator = new CacheOptionsCreator();
        var result = cacheOptionsCreator.Create(fileCacheOptions, null, null, globalConfiguration);

        result.TtlSeconds.ShouldBe(fileCacheOptions.TtlSeconds.Value);
        result.Region.ShouldBe(fileCacheOptions.Region);
        result.Header.ShouldBe(fileCacheOptions.Header);
        result.EnableContentHashing.ShouldBe(fileCacheOptions.EnableContentHashing.Value);
    }

    [Fact]
    [Trait("Issue", "2059")]
    public void ShouldCreateCacheOptionsWithDefaults()
    {
        var cacheOptionsCreator = new CacheOptionsCreator();
        var result = cacheOptionsCreator.Create(new FileCacheOptions(), "/", new List<string> { "GET" }, null);

        result.TtlSeconds.ShouldBe(0);
        result.Region.ShouldBe("GET");
        result.Header.ShouldBe(null);
        result.EnableContentHashing.ShouldBe(false);
    }

    [Fact]
    [Trait("Issue", "2059")]
    public void ShouldComputeRegionIfNotProvided()
    {
        var globalConfiguration = GlobalConfigurationFactory();
        var fileCacheOptions = FileCacheOptionsFactory();

        globalConfiguration.CacheOptions.Region = null;
        fileCacheOptions.Region = null;

        var cacheOptionsCreator = new CacheOptionsCreator();
        var result = cacheOptionsCreator.Create(fileCacheOptions, "/api/values", new List<string> { "GET", "POST" }, globalConfiguration);

        result.TtlSeconds.ShouldBe(fileCacheOptions.TtlSeconds.Value);
        result.Region.ShouldBe("GETPOSTapivalues");
        result.Header.ShouldBe(fileCacheOptions.Header);
        result.EnableContentHashing.ShouldBe(fileCacheOptions.EnableContentHashing.Value);
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
