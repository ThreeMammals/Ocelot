using Ocelot.Configuration.File;

namespace Ocelot.Cache;

public class RegionCreator : IRegionCreator
{
    public string Create(FileCacheOptions fileCacheOptions, string upstreamPathTemplate, IList<string> upstreamHttpMethod)
    {
        if (!string.IsNullOrEmpty(fileCacheOptions?.Region))
        {
            return fileCacheOptions?.Region;
        }

        var methods = string.Join(string.Empty, upstreamHttpMethod.Select(m => m));

        var region = $"{methods}{upstreamPathTemplate.Replace("/", string.Empty)}";

        return region;
    }
}
