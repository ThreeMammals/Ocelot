using System;
using System.IO;
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
        
        public Response<FileConfiguration> Get()
        {
            var fileConfig = _repo.Get();
            return new OkResponse<FileConfiguration>(fileConfig.Data);
        }
    }
}