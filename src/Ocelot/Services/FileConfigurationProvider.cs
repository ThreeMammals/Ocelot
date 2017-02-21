using System;
using System.IO;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Services
{
    public class FileConfigurationProvider : IFileConfigurationProvider
    {
        public Response<FileConfiguration> Get()
        {
            var configFilePath = $"{AppContext.BaseDirectory}/configuration.json";
            var json = File.ReadAllText(configFilePath);
            var fileConfiguration = JsonConvert.DeserializeObject<FileConfiguration>(json);
            return new OkResponse<FileConfiguration>(fileConfiguration);
        }
    }
}