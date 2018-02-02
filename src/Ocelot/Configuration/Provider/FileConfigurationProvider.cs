using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.Configuration.Provider
{
    public class FileConfigurationProvider : IFileConfigurationProvider
    {
        private IFileConfigurationRepository _repo;

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