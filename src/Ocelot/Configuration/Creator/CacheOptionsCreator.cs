using Ocelot.Cache;
using Ocelot.Configuration.File;
using System.Diagnostics;

namespace Ocelot.Configuration.Creator;

public class CacheOptionsCreator : ICacheOptionsCreator
{
    private readonly IRegionCreator _regionCreator;
    public CacheOptionsCreator(IRegionCreator regionCreator)
    {
        _regionCreator = regionCreator ?? throw new ArgumentNullException(nameof(regionCreator));
    }

    public CacheOptions Create(FileCacheOptions fileCacheOptions, string upstreamPathTemplate, IList<string> upstreamHttpMethods, FileGlobalConfiguration globalConfiguration)
    {
        var region = _regionCreator.Create(fileCacheOptions.Region ?? globalConfiguration.CacheOptions.Region, upstreamPathTemplate, upstreamHttpMethods);
        var header = fileCacheOptions.Header ?? globalConfiguration.CacheOptions.Header;
        var ttlSeconds = fileCacheOptions.TtlSeconds ?? globalConfiguration.CacheOptions.TtlSeconds;
        var enableContentHashing = fileCacheOptions.EnableContentHashing ?? globalConfiguration.CacheOptions.EnableContentHashing;

        return new CacheOptions(ttlSeconds, region, header, enableContentHashing);
    }
}
