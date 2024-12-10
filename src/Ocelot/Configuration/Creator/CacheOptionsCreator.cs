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
        var enableHeadersHashing = options.EnableHeadersHashing ?? global?.CacheOptions.EnableHeadersHashing;
        var cleanableHashingRegexes = options.CleanableHashingRegexes ?? global?.CacheOptions.CleanableHashingRegexes;

        return new CacheOptions(ttlSeconds, region, header, enableContentHashing, enableHeadersHashing, cleanableHashingRegexes);
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
