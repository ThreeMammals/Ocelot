using System.Linq;
using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.Cache
{

    public class RegionCreator : IRegionCreator
    {
        public string Create(FileReRoute reRoute)
        {
            if(!string.IsNullOrEmpty(reRoute?.FileCacheOptions?.Region))
            {
                return reRoute?.FileCacheOptions?.Region;
            }

            var methods = string.Join("", reRoute.UpstreamHttpMethod.Select(m => m));

            var region = $"{methods}{reRoute.UpstreamPathTemplate.Replace("/", "")}";
            
            return region;
        }
    }
}