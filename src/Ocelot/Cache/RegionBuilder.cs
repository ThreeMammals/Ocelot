using System.Linq;
using Ocelot.Configuration;

namespace Ocelot.Cache
{
    public interface IRegionCreator
    {
        string Region(ReRoute reRoute);
    }

    public class RegionCreator : IRegionCreator
    {
        public string Region(ReRoute reRoute)
        {
            var methods = string.Join("", reRoute.UpstreamHttpMethod.Select(m => m.Method));

            var region = $"{methods}{reRoute.UpstreamPathTemplate.Value.Replace("/", "")}";
            
            return region;
        }
    }
}