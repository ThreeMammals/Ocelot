using Ocelot.Cache;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class CacheOptionsCreator : ICacheOptionsCreator
{
    private readonly IRegionCreator _regionCreator;
    public CacheOptionsCreator(IRegionCreator regionCreator)
    {
        _regionCreator = regionCreator ?? throw new ArgumentNullException(nameof(regionCreator));
    }


    public CacheOptions Create(FileCacheOptions fileCacheOptions, string upstreamPathTemplate, IList<string> upstreamHttpMethods)
    {
        var region = _regionCreator.Create(fileCacheOptions, upstreamPathTemplate, upstreamHttpMethods);

        return new CacheOptions(fileCacheOptions.TtlSeconds, region, fileCacheOptions.Header, fileCacheOptions.EnableContentHashing);
    }
}
