using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.Configuration.Provider
{
    public class FileConfigurationProvider : IFileConfigurationProvider
    {
        private readonly IFileConfigurationRepository _repo;

        public FileConfigurationProvider(IFileConfigurationRepository repo)
        {
            _repo = repo;
        }
        
        public async Task<Response<FileConfiguration>> Get()
        {
            var fileConfig = await _repo.Get();
            return new OkResponse<FileConfiguration>(fileConfig.Data);
        }
    }
}
