using System.Threading.Tasks;
using Ocelot.Configuration.Creator;
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
        private readonly IOcelotConfigurationCreator _creator;

        public OcelotConfigurationProvider(IOcelotConfigurationRepository repo, 
            IOcelotConfigurationCreator creator)
        {
            _repo = repo;
            _creator = creator;
        }

        public async Task<Response<IOcelotConfiguration>> Get()
        {
            var repoConfig = _repo.Get();

            if (repoConfig.IsError)
            {
                return new ErrorResponse<IOcelotConfiguration>(repoConfig.Errors);
            }

            if (repoConfig.Data == null)
            {
                var creatorConfig = await _creator.Create();

                if (creatorConfig.IsError)
                {
                    return new ErrorResponse<IOcelotConfiguration>(creatorConfig.Errors);
                }

                _repo.AddOrReplace(creatorConfig.Data);

                return new OkResponse<IOcelotConfiguration>(creatorConfig.Data);
            }

            return new OkResponse<IOcelotConfiguration>(repoConfig.Data);
        }
    }
}