using System.Threading.Tasks;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.Configuration.Setter
{
    public class FileConfigurationSetter : IFileConfigurationSetter
    {
        private readonly IOcelotConfigurationRepository _configRepo;
        private readonly IOcelotConfigurationCreator _configCreator;

        public  FileConfigurationSetter(IOcelotConfigurationRepository configRepo, IOcelotConfigurationCreator configCreator)
        {
            _configRepo = configRepo;
            _configCreator = configCreator;
        }

        public async Task<Response> Set(FileConfiguration fileConfig)
        {
            var config = await _configCreator.Create(fileConfig);

            if(!config.IsError)
            {
                _configRepo.AddOrReplace(config.Data);
            }

            return new ErrorResponse(config.Errors);
        }
    }
}