using System;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    public class FileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        private static readonly object _lock = new object();

        public FileConfigurationRepository(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public Response<FileConfiguration> Get()
        {
            var configFilePath = $"{AppContext.BaseDirectory}/configuration.{_hostingEnvironment.EnvironmentName}.json";
            string json = string.Empty;
            lock(_lock)
            {
                json = System.IO.File.ReadAllText(configFilePath);
            }
            var fileConfiguration = JsonConvert.DeserializeObject<FileConfiguration>(json);
            return new OkResponse<FileConfiguration>(fileConfiguration);
        }

        public Response Set(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{AppContext.BaseDirectory}/configuration.{_hostingEnvironment.EnvironmentName}.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            lock(_lock)
            {
                if (System.IO.File.Exists(configurationPath))
                {
                    System.IO.File.Delete(configurationPath);
                }

                System.IO.File.WriteAllText(configurationPath, jsonConfiguration);
            }
            
            return new OkResponse();
        }
    }
}