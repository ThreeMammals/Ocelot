using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class CacheOptionsCreator : ICacheOptionsCreator
{
    public CacheOptions Create(FileCacheOptions fileCacheOptions, string upstreamPathTemplate, IList<string> upstreamHttpMethods, FileGlobalConfiguration globalConfiguration)
    {
        var region = GetRegion(fileCacheOptions.Region ?? globalConfiguration.CacheOptions.Region, upstreamPathTemplate, upstreamHttpMethods);
        var header = fileCacheOptions.Header ?? globalConfiguration.CacheOptions.Header;
        var ttlSeconds = fileCacheOptions.TtlSeconds ?? globalConfiguration.CacheOptions.TtlSeconds;
        var enableContentHashing = fileCacheOptions.EnableContentHashing ?? globalConfiguration.CacheOptions.EnableContentHashing;

        return new CacheOptions(ttlSeconds, region, header, enableContentHashing);
    }

    protected virtual string GetRegion(string region, string upstreamPathTemplate, IList<string> upstreamHttpMethod)
    {
        if (!string.IsNullOrEmpty(region))
        {
            return region;
        }

        var methods = string.Join(string.Empty, upstreamHttpMethod.Select(m => m));
        return $"{methods}{upstreamPathTemplate.Replace("/", string.Empty)}";
    }
}
