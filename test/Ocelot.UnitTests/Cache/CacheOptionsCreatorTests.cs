using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Cache;

public class CacheOptionsCreatorTests : UnitTest
{
    private CacheOptions _cacheOptions;
    private FileRoute _route;

    [Fact]
    public void should_create_region()
    {
        var route = new FileRoute
        {
            UpstreamHttpMethod = new List<string> { "Get" },
            UpstreamPathTemplate = "/testdummy",
        };

        this.Given(_ => GivenTheRoute(route))
            .When(_ => WhenICreateTheRegion())
            .Then(_ => ThenTheRegionIs("Gettestdummy"))
            .BDDfy();
    }

    [Fact]
    public void should_use_region()
    {
        var route = new FileRoute
        {
            FileCacheOptions = new FileCacheOptions
            {
                Region = "region",
            },
        };

        this.Given(_ => GivenTheRoute(route))
            .When(_ => WhenICreateTheRegion())
            .Then(_ => ThenTheRegionIs("region"))
            .BDDfy();
    }

    private void GivenTheRoute(FileRoute route)
    {
        _route = route;
    }

    private void WhenICreateTheRegion()
    {
        var cacheOptionsCreator = new CacheOptionsCreator();
        _cacheOptions = cacheOptionsCreator.Create(_route.FileCacheOptions, new FileGlobalConfiguration(), _route.UpstreamPathTemplate, _route.UpstreamHttpMethod);
    }

    private void ThenTheRegionIs(string expected)
    {
        _cacheOptions.Region.ShouldBe(expected);
    }
}
