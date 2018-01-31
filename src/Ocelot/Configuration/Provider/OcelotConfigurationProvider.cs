using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.Configuration.Provider
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class OcelotConfigurationProvider : IOcelotConfigurationProvider
    {
        private readonly IOcelotConfigurationRepository _config;

        public OcelotConfigurationProvider(IOcelotConfigurationRepository repo)
        {
            _config = repo;
        }

        public async Task<Response<IOcelotConfiguration>> Get()
        {
            var repoConfig = await _config.Get();

            if (repoConfig.IsError)
            {
                return new ErrorResponse<IOcelotConfiguration>(repoConfig.Errors);
            }

            return new OkResponse<IOcelotConfiguration>(repoConfig.Data);
        }
    }
}