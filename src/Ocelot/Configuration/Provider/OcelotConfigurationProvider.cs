using System.Threading.Tasks;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.Configuration.Provider
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class OcelotConfigurationProvider : IOcelotConfigurationProvider
    {
        private readonly IOcelotConfigurationRepository _repo;

        public OcelotConfigurationProvider(IOcelotConfigurationRepository repo)
        {
            _repo = repo;
        }

        public async Task<Response<IOcelotConfiguration>> Get()
        {
            var repoConfig = await _repo.Get();

            if (repoConfig.IsError)
            {
                return new ErrorResponse<IOcelotConfiguration>(repoConfig.Errors);
            }

            return new OkResponse<IOcelotConfiguration>(repoConfig.Data);
        }
    }
}