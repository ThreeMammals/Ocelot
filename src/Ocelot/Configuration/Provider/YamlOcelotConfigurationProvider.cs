using Ocelot.Configuration.Creator;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.Configuration.Provider
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class YamlOcelotConfigurationProvider : IOcelotConfigurationProvider
    {
        private readonly IOcelotConfigurationRepository _repo;
        private readonly IOcelotConfigurationCreator _creator;

        public YamlOcelotConfigurationProvider(IOcelotConfigurationRepository repo, 
            IOcelotConfigurationCreator creator)
        {
            _repo = repo;
            _creator = creator;
        }

        public Response<IOcelotConfiguration> Get()
        {
            var config = _repo.Get();

            if (config.IsError)
            {
                return new ErrorResponse<IOcelotConfiguration>(config.Errors);
            }

            if (config.Data == null)
            {
                var configuration = _creator.Create();

                if (configuration.IsError)
                {
                    return new ErrorResponse<IOcelotConfiguration>(configuration.Errors);
                }

                _repo.AddOrReplace(configuration.Data);

                return new OkResponse<IOcelotConfiguration>(configuration.Data);
            }

            return new OkResponse<IOcelotConfiguration>(config.Data);
        }
    }
}