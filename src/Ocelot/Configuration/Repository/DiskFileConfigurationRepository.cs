using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    public class DiskFileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly string _configFilePath;

        private static readonly object _lock = new object();

        private const string ConfigurationFileName = "ocelot";

        public DiskFileConfigurationRepository(IHostingEnvironment hostingEnvironment)
        {
            _configFilePath = $"{AppContext.BaseDirectory}/{ConfigurationFileName}{(string.IsNullOrEmpty(hostingEnvironment.EnvironmentName) ? string.Empty : ".")}{hostingEnvironment.EnvironmentName}.json";
        }

        public Task<Response<FileConfiguration>> Get()
        {
            string jsonConfiguration;

            lock(_lock)
            {
                jsonConfiguration = System.IO.File.ReadAllText(_configFilePath);
            }

            var fileConfiguration = JsonConvert.DeserializeObject<FileConfiguration>(jsonConfiguration);

            return Task.FromResult<Response<FileConfiguration>>(new OkResponse<FileConfiguration>(fileConfiguration));
        }

        public Task<Response> Set(FileConfiguration fileConfiguration)
        {
            string jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);

            lock(_lock)
            {
                if (System.IO.File.Exists(_configFilePath))
                {
                    System.IO.File.Delete(_configFilePath);
                }

                System.IO.File.WriteAllText(_configFilePath, jsonConfiguration);
            }

            return Task.FromResult<Response>(new OkResponse());
        }
    }
}
