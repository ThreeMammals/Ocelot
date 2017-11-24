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
        private readonly IFileConfigurationRepository _repo;

        public  FileConfigurationSetter(IOcelotConfigurationRepository configRepo, 
            IOcelotConfigurationCreator configCreator, IFileConfigurationRepository repo)
        {
            _configRepo = configRepo;
            _configCreator = configCreator;
            _repo = repo;
        }

        public async Task<Response> Set(FileConfiguration fileConfig)
        {
            var response = await _repo.Set(fileConfig);

            if(response.IsError)
            {
                return new ErrorResponse(response.Errors);
            }

            var config = await _configCreator.Create(fileConfig);

            if(!config.IsError)
            {
                await _configRepo.AddOrReplace(config.Data);
            }

            return new ErrorResponse(config.Errors);
        }
    }
}