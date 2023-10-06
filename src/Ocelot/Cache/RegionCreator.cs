using Ocelot.Configuration.File;

namespace Ocelot.Cache
{
    public class RegionCreator : IRegionCreator
    {
        public string Create(FileRoute route)
        {
            if (!string.IsNullOrEmpty(route?.FileCacheOptions?.Region))
            {
                return route?.FileCacheOptions?.Region;
            }

            var methods = string.Join(string.Empty, route.UpstreamHttpMethod.Select(m => m));

            var region = $"{methods}{route.UpstreamPathTemplate.Replace("/", string.Empty)}";

            return region;
        }
    }
}
