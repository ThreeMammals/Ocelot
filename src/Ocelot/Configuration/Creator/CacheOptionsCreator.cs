using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class CacheOptionsCreator : ICacheOptionsCreator
{
    public CacheOptions Create(FileCacheOptions options, FileGlobalConfiguration global, string upstreamPathTemplate, IList<string> upstreamHttpMethods)
    {
        var region = GetRegion(options.Region ?? global?.CacheOptions.Region, upstreamPathTemplate, upstreamHttpMethods);
        var header = options.Header ?? global?.CacheOptions.Header;
        var ttlSeconds = options.TtlSeconds ?? global?.CacheOptions.TtlSeconds;
        var enableContentHashing = options.EnableContentHashing ?? global?.CacheOptions.EnableContentHashing;
        var enableFlexibleHashing = options.EnableFlexibleHashing ?? global?.CacheOptions.EnableFlexibleHashing;
        var flexibleHashingRegexes = options.FlexibleHashingRegexes ?? global?.CacheOptions.FlexibleHashingRegexes;

        return new CacheOptions(ttlSeconds, region, header, enableContentHashing, enableFlexibleHashing, flexibleHashingRegexes);
    }

    protected virtual string GetRegion(string region, string upstreamPathTemplate, IList<string> upstreamHttpMethod)
    {
        if (!string.IsNullOrEmpty(region))
        {
            return region;
        }

        var methods = string.Join(string.Empty, upstreamHttpMethod);
        return $"{methods}{upstreamPathTemplate.Replace("/", string.Empty)}";
    }
}
