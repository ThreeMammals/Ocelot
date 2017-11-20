using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    public class FileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly string _configFilePath;

        private static readonly object _lock = new object();
        
        public FileConfigurationRepository(IHostingEnvironment hostingEnvironment)
        {
            _configFilePath = $"{AppContext.BaseDirectory}/configuration{(string.IsNullOrEmpty(hostingEnvironment.EnvironmentName) ? string.Empty : ".")}{hostingEnvironment.EnvironmentName}.json";
        }

        public async Task<Response<FileConfiguration>> Get()
        {
            string jsonConfiguration;

            lock(_lock)
            {
                jsonConfiguration = System.IO.File.ReadAllText(_configFilePath);
            }

            var fileConfiguration = JsonConvert.DeserializeObject<FileConfiguration>(jsonConfiguration);

            return new OkResponse<FileConfiguration>(fileConfiguration);
        }

        public async Task<Response> Set(FileConfiguration fileConfiguration)
        {
            string jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            lock(_lock)
            {
                if (System.IO.File.Exists(_configFilePath))
                {
                    System.IO.File.Delete(_configFilePath);
                }

                System.IO.File.WriteAllText(_configFilePath, jsonConfiguration);
            }
            
            return new OkResponse();
        }
    }
}