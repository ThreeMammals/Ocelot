using Ocelot.Configuration.File;

namespace Ocelot.Cache;

public class RegionCreator : IRegionCreator
{
    public string Create(string region, string upstreamPathTemplate, IList<string> upstreamHttpMethod)
    {
        if (!string.IsNullOrEmpty(region))
        {
            return region;
        }

        var methods = string.Join(string.Empty, upstreamHttpMethod.Select(m => m));
        var computedRegion = $"{methods}{upstreamPathTemplate.Replace("/", string.Empty)}";

        return computedRegion;
    }
}
