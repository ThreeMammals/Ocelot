using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class CacheOptionsCreator : ICacheOptionsCreator
{
    public CacheOptions Create(FileCacheOptions options, FileGlobalConfiguration globalConfiguration, string upstreamPathTemplate, IReadOnlyCollection<string> upstreamHttpMethods)
    {
        options ??= new();
        var global = globalConfiguration?.CacheOptions ?? new();
        var region = GetRegion(options.Region.IfEmpty(global.Region), upstreamPathTemplate, upstreamHttpMethods);
        var header = options.Header.IfEmpty(global.Header);
        var ttlSeconds = options.TtlSeconds ?? global.TtlSeconds;
        var enableHashing = options.EnableContentHashing ?? global.EnableContentHashing;

        return new CacheOptions(ttlSeconds, region, header, enableHashing);
    }

    protected virtual string GetRegion(string region, string upstreamPathTemplate, IReadOnlyCollection<string> upstreamHttpMethod)
    {
        if (!string.IsNullOrEmpty(region))
        {
            return region;
        }

        var methods = string.Join(string.Empty, upstreamHttpMethod);
        return $"{methods}{upstreamPathTemplate.Replace("/", string.Empty)}";
    }
}
