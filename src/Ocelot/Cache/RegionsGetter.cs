using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration.Provider;
using Ocelot.Logging;

namespace Ocelot.Cache
{
    public interface IRegionsGetter
    {
        Task<List<string>> Regions();
    }
    public class RegionsGetter : IRegionsGetter
    {
        private readonly IOcelotConfigurationProvider _provider;
        private readonly IRegionCreator _creator;
        private readonly IOcelotLogger _logger;

        public RegionsGetter(IOcelotConfigurationProvider provider, IRegionCreator creator, IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RegionsGetter>();
            _provider = provider;
            _creator = creator;
        }

        public async Task<List<string>> Regions()
        {
            var config = await _provider.Get();

            if(config.IsError)
            {
                _logger.LogError("unable to find regions", new Exception(string.Join(",", config.Errors)));
                return new List<string>();
            }

            var cachedReRoutes = config.Data.ReRoutes.Where(x => x.IsCached);
            
            var regions = new List<string>();

            foreach(var reRoute in cachedReRoutes)
            {
                var region = _creator.Region(reRoute);
                regions.Add(region);
            }

            return regions;
        }
    }
}