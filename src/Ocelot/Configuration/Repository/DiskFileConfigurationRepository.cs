using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Ocelot.Cache;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Responses;
using FileSys = System.IO.File;

namespace Ocelot.Configuration.Repository
{
    public class DiskFileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IOcelotConfigurationChangeTokenSource _changeTokenSource;
        private readonly IOcelotCache<FileConfiguration> _cache;
        private readonly string _cacheKey;
        private FileInfo _ocelotFile;
        private FileInfo _environmentFile;
        private readonly object _lock = new();

        public DiskFileConfigurationRepository(IWebHostEnvironment hostingEnvironment,
            IOcelotConfigurationChangeTokenSource changeTokenSource, Cache.IOcelotCache<FileConfiguration> cache)
        {
            _hostingEnvironment = hostingEnvironment;
            _changeTokenSource = changeTokenSource;
            Initialize(AppContext.BaseDirectory);
            _cache = cache;
            _cacheKey = "InternalDiskFileConfigurationRepository";
        }

        public DiskFileConfigurationRepository(IWebHostEnvironment hostingEnvironment, IOcelotConfigurationChangeTokenSource changeTokenSource, string folder)
        {
            _hostingEnvironment = hostingEnvironment;
            _changeTokenSource = changeTokenSource;
            Initialize(folder);
        }

        private void Initialize(string folder)
        {
            folder ??= AppContext.BaseDirectory;
            _ocelotFile = new FileInfo(Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile));
            var envFile = !string.IsNullOrEmpty(_hostingEnvironment.EnvironmentName)
                ? string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, _hostingEnvironment.EnvironmentName)
                : ConfigurationBuilderExtensions.PrimaryConfigFile;
            _environmentFile = new FileInfo(Path.Combine(folder, envFile));
        }

        public Task<Response<FileConfiguration>> Get()
        {
            var configuration = _cache.Get(_cacheKey, _cacheKey);

            if (configuration != null)
                return Task.FromResult<Response<FileConfiguration>>(new OkResponse<FileConfiguration>(configuration));

            string jsonConfiguration;

            lock (_lock)
            {
                jsonConfiguration = FileSys.ReadAllText(_environmentFile.FullName);
            }

            var fileConfiguration = JsonConvert.DeserializeObject<FileConfiguration>(jsonConfiguration);

            return Task.FromResult<Response<FileConfiguration>>(new OkResponse<FileConfiguration>(fileConfiguration));
        }

        public Task<Response> Set(FileConfiguration fileConfiguration)
        {
            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);

            lock (_lock)
            {
                if (_environmentFile.Exists)
                {
                    _environmentFile.Delete();
                }

                FileSys.WriteAllText(_environmentFile.FullName, jsonConfiguration);

                if (_ocelotFile.Exists)
                {
                    _ocelotFile.Delete();
                }

                FileSys.WriteAllText(_ocelotFile.FullName, jsonConfiguration);
            }

            _changeTokenSource.Activate();

            _cache.AddAndDelete(_cacheKey, fileConfiguration, TimeSpan.FromMinutes(5), _cacheKey);

            return Task.FromResult<Response>(new OkResponse());
        }
    }
}
